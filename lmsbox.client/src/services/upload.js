// Upload service: placeholder endpoints that your backend should implement
// - POST /api/admin/uploads/media (FormData: file) -> { url }
// - POST /api/admin/uploads/scorm (FormData: file) -> { packageId, entryUrl, version }
// Supports progress via XMLHttpRequest since fetch does not report upload progress.

export function uploadMedia(file, onProgress) {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open('POST', '/api/admin/uploads/media');
    xhr.responseType = 'json';

    xhr.upload.onprogress = (evt) => {
      if (evt.lengthComputable && typeof onProgress === 'function') {
        const percent = Math.round((evt.loaded / evt.total) * 100);
        onProgress(percent);
      }
    };
    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        const res = xhr.response || {};
        if (res.url) {
          resolve(res);
        } else {
          reject(new Error('Upload completed but no URL returned'));
        }
      } else {
        reject(new Error(`Upload failed with status ${xhr.status}`));
      }
    };
    xhr.onerror = () => reject(new Error('Network error during upload'));

    const form = new FormData();
    form.append('file', file);
    xhr.send(form);
  });
}

export function uploadScorm(file, onProgress) {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open('POST', '/api/admin/uploads/scorm');
    xhr.responseType = 'json';

    xhr.upload.onprogress = (evt) => {
      if (evt.lengthComputable && typeof onProgress === 'function') {
        const percent = Math.round((evt.loaded / evt.total) * 100);
        onProgress(percent);
      }
    };
    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        const res = xhr.response || {};
        if (res.entryUrl) {
          resolve(res);
        } else {
          reject(new Error('Upload completed but no entryUrl returned'));
        }
      } else {
        reject(new Error(`Upload failed with status ${xhr.status}`));
      }
    };
    xhr.onerror = () => reject(new Error('Network error during upload'));

    const form = new FormData();
    form.append('file', file);
    xhr.send(form);
  });
}
