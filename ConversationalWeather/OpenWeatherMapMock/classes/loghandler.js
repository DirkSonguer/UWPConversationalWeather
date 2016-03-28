// *************************************************** //
// Loghandler class
//
// This script is a super simple logger.
// Yes, I know I should use Winston or Bunyan, but this
// is a PoC, ok?
// *************************************************** //

// node utilities
var util = require('util');

class LoghandlerClass {
    constructor() {
	}

	// send a message to a specific socket
	log(message, severity) {
		// convert from object to string	
		if (typeof message === 'object') {
			message = util.inspect(message, { depth: null });
		}

		if (severity >= 0) {
			// define colours
			let messagecolor = "\x1b[0m";
			if (severity == 2) messagecolor = "\x1b[32m";
			if (severity == 3) messagecolor = "\x1b[35m";
			if (severity > 3) messagecolor = "\x1b[31m";

			// write message		
			console.log(messagecolor + 'L' + severity + ' # ' + Date.now() + ' # ' + process.pid + ' # ' + message);
		}

		// done
		return true;
	}
}

// export default new LoghandlerClass();
var logHandler = new LoghandlerClass();
module.exports = logHandler;