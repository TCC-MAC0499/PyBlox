from google.cloud import storage
from datetime import datetime
import functions_framework
import json

@functions_framework.http
def detect_block_borders(request):
    """HTTP Cloud Function.
    Args:
        request (flask.Request): The request object.
        <https://flask.palletsprojects.com/en/1.1.x/api/#incoming-request-data>
    Returns:
        The response text, or any set of values that can be turned into a
        Response object using `make_response`
        <https://flask.palletsprojects.com/en/1.1.x/api/#flask.make_response>.
    """

    try:
        image_bytes = request.data
        if not image_bytes:
            return (json.dumps({"success": False, "error": "Field 'image_bytes' was not found in the request body."}), 4000, {"Content-Type": "application/json"})

        google_storage = storage.Client()
        bucket = google_storage.bucket("border-detector-sources")

        blob = bucket.blob(f"source_{datetime.utcnow().strftime('%Y%m%d_%H%M%S')}.jpg")
        blob.upload_from_string(image_bytes, content_type="image/jpeg")
        return (json.dumps({"success": True, "image_url": blob.public_url}), 200, {"Content-Type": "application/json"})

    except Exception as e:
        print(f"Error uploading image to bucket: {e}")
        return (json.dumps({"success": False, "error": str(e)}), 500, {"Content-Type": "application/json"})