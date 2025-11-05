// SCORM 1.2 API Implementation
var API = {
    data: {
        lessonStatus: "not attempted",
        score: "",
        lessonLocation: "",
        suspendData: ""
    },
    
    LMSInitialize: function(param) {
        console.log('SCORM: LMSInitialize called');
        this.data.lessonStatus = "incomplete";
        return "true";
    },
    
    LMSFinish: function(param) {
        console.log('SCORM: LMSFinish called');
        // Send completion status to parent window
        this.notifyParent();
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
                value = this.data.lessonLocation;
                break;
            case "cmi.core.lesson_status":
                value = this.data.lessonStatus;
                break;
            case "cmi.core.score.raw":
                value = this.data.score;
                break;
            case "cmi.suspend_data":
                value = this.data.suspendData;
                break;
            default:
                value = "";
        }
        return value;
    },
    
    LMSSetValue: function(element, value) {
        console.log('SCORM: LMSSetValue called', element, value);
        
        switch(element) {
            case "cmi.core.lesson_status":
                this.data.lessonStatus = value;
                // Notify parent if status is completed or passed
                if (value === "completed" || value === "passed") {
                    this.notifyParent();
                }
                break;
            case "cmi.core.score.raw":
                this.data.score = value;
                break;
            case "cmi.core.lesson_location":
                this.data.lessonLocation = value;
                break;
            case "cmi.suspend_data":
                this.data.suspendData = value;
                break;
        }
        
        return "true";
    },
    
    LMSCommit: function(param) {
        console.log('SCORM: LMSCommit called');
        // Commit changes and notify parent if completed
        if (this.data.lessonStatus === "completed" || this.data.lessonStatus === "passed") {
            this.notifyParent();
        }
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
    },
    
    notifyParent: function() {
        // Send message to parent window about completion
        if (window.parent && window.parent !== window) {
            window.parent.postMessage({
                type: 'scorm',
                status: this.data.lessonStatus,
                score: this.data.score
            }, '*');
            console.log('SCORM: Notified parent window of completion', this.data.lessonStatus);
        }
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
