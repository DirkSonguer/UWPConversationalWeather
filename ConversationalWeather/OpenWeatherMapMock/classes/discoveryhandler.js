// *************************************************** //
// DiscoveryhandlerClass class
//
// This script takes care of Multicast DNS service
// discovery.
// *************************************************** //

// winston logger
var winston = require('winston');

// promising using bluebird
var Promise = require('bluebird');

// bonjour class
var bonjour = require('bonjour')();

class DiscoveryhandlerClass {
    /**
     * Get the first Mnemosyne Configuration address with Multicast DNS service discovery
     * If many Mnemosyne Configuration services are available the irst one will be returned.
     * @param {Integer} timeout - Timeout in ms for requestiing the URL of Mnemosyne Configuration with mDNS.
     * returns {Object} - Promised service object of the Mnemosyne Configuration service (s. https://github.com/watson/bonjour).
     */
    getFirstMnemosyneConfigurationUrl(timeout) {
        return new Promise(function (resolve, reject) {
            // set timeout based on parameter
            setTimeout(function () {
                reject("Timeout while requesting URL of the Mnemosyne Configuration service with mDNS")
            }, timeout);

            // browse for all titan services
            bonjour.findOne({ type: 'titan' }, function (service) {
                // check if at least one address was found
                if (service.addresses.length > 0) {
                    // return the service
                    resolve(service);
                }

                // no service was found
                reject("Could not determine the URL of the Mnemosyne Configuration service with mDNS");
            });

        });
    }
}

// export the class as singleton
var discoveryHandler = new DiscoveryhandlerClass();
module.exports = discoveryHandler;