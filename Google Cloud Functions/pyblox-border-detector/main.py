import cv2
import functions_framework
import json
import numpy as np
from google.cloud import storage
from datetime import datetime

@functions_framework.http
def detect_block_borders(request):
    """
    Detect block borders with computer vision. Algorithm inspired by:
    - https://yasoob.me/2015/03/11/a-guide-to-finding-books-in-images-using-python-and-opencv/
    """

    try:
        frame_bytes = request.data
        if not frame_bytes:
            return (json.dumps({
                "success": False,
                "error": "Field 'frame_bytes' was not found in the request body."
                }), 4000, {"Content-Type": "application/json"})

        frame_np = np.frombuffer(frame_bytes, dtype=np.uint8)
        img = cv2.imdecode(frame_np, cv2.IMREAD_COLOR)
        if img is None:
            return ("Cannot decode frame image", 400)

        # Convert colorful image into grayscale
        img_gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        # Apply Gaussian blur to image to minimize noise
        img_gray_gauss = cv2.GaussianBlur(img_gray, (5,5), 0)

        # Apply Canny's edge detection algorithm
        # For more information, see https://docs.opencv.org/4.x/da/d22/tutorial_py_canny.html)
        img_edges = cv2.Canny(img_gray_gauss, 50, 110)

        # Apply closing morphological transformation, useful for closing small fissures in edges
        # For more information, see https://docs.opencv.org/3.4/d4/d76/tutorial_js_morphological_ops.html
        square_kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (5, 5))
        img_closed_edges = cv2.morphologyEx(img_edges, cv2.MORPH_CLOSE, square_kernel)

        # Find the outermost contours in the image with closed edges
        # For more information, see https://docs.opencv.org/4.x/d4/d73/tutorial_py_contours_begin.html
        # and https://docs.opencv.org/4.x/d3/dc0/group__imgproc__shape.html#ga819779b9857cc2f8601e6526a3a5bc71
        (img_contours, contour_hierarchy) = cv2.findContours(img_closed_edges.copy(), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

        img_approx_contours = []
        for contour in img_contours:
            # Calculate the contour's perimeter
            perimeter = cv2.arcLength(contour, True)
            # Approximate the contour to avoid detecting more contour sides due to noise
            approx_contour = cv2.approxPolyDP(contour, 0.02 * perimeter, True)
            # Filter only four-sided contours and assume them to be blocks
            if len(approx_contour) == 4:
                img_approx_contours.append(approx_contour)

        response_approx_contours = _convert_nested_array_to_json(img_approx_contours)

        # START OF DEBUGGING: Remove later to avoid Google Cloud Storage charges
        # Draw the contours in the source image
        cv2.drawContours(img, img_approx_contours, -1, (0, 255, 0), 4)
        img_bytes = cv2.imencode('.png', img)[1].tostring()

        google_storage = storage.Client()
        bucket = google_storage.bucket("border-detector-sources")
        blob = bucket.blob(f"source_{datetime.utcnow().strftime('%Y%m%d_%H%M%S')}.jpg")
        blob.upload_from_string(img_bytes, content_type="image/jpeg")

        print(response_approx_contours)
        # END OF DEBUGGING

        return (json.dumps({
            "success": True,
            "block_borders": response_approx_contours
            }), 200, {"Content-Type": "application/json"})

    except Exception as e:
        return (json.dumps({
            "success": False,
            "error": str(e)
            }), 500, {"Content-Type": "application/json"})

def _convert_nested_array_to_json(block_borders_array):
    block_borders_json = []
    for block_border in block_borders_array:
        border = []
        for border_coordinates in block_border:
            flat_border_coordinates = border_coordinates[0].tolist()
            border.append({
                "x": flat_border_coordinates[0],
                "y": flat_border_coordinates[1]
            })
        block_borders_json.append({
            "border": border
        })
    return block_borders_json