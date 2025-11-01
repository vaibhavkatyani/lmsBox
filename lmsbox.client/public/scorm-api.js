// SCORM 1.2 API Implementation
var API = {
    LMSInitialize: function(param) {
        console.log('SCORM: LMSInitialize called');
        return "true";
    },
    LMSFinish: function(param) {
        console.log('SCORM: LMSFinish called');
        return "true";
    },
    LMSGetValue: function(element) {
        console.log('SCORM: LMSGetValue called for', element);
        var value = "";
        switch(element) {
            case "cmi.core.student_id":
                value = "student_001";
                break;
            case "cmi.core.student_name":
                value = "Student Name";
                break;
            case "cmi.core.lesson_location":
                value = "";
                break;
            case "cmi.core.lesson_status":
                value = "not attempted";
                break;
            case "cmi.core.score.raw":
                value = "";
                break;
            case "cmi.suspend_data":
                value = "";
                break;
            default:
                value = "";
        }
        return value;
    },
    LMSSetValue: function(element, value) {
        console.log('SCORM: LMSSetValue called', element, value);
        return "true";
    },
    LMSCommit: function(param) {
        console.log('SCORM: LMSCommit called');
        return "true";
    },
    LMSGetLastError: function() {
        return "0";
    },
    LMSGetErrorString: function(errorCode) {
        return "No error";
    },
    LMSGetDiagnostic: function(errorCode) {
        return "No error";
    }
};

// Make API available globally
window.API = API;

// Load the SCORM content when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    var scormUrl = new URLSearchParams(window.location.search).get('url');
    if (scormUrl) {
        document.getElementById('scorm-iframe').src = scormUrl;
    }
});
