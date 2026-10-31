import urllib.request


def application(environ, start_response):
    url = f"http://127.0.0.1:{environ['SERVER_PORT']}/command/health/healthz"
    try:
        version = urllib.request.urlopen(url, timeout=3).read()
    except Exception as e:
        start_response("503 Service Unavailable", [("Content-Type", "text/plain")])
        return [f"cgi: {e}\n".encode()]
    start_response("200 OK", [("Content-Type", "text/plain")])
    return [version]
