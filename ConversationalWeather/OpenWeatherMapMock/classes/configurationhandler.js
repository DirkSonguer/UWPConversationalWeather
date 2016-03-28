// *************************************************** //
// Configurationhandler class
//
// This script takes care of loading and providing the
// general configuration from Mnemosyne Configuration.
// *************************************************** //

// winston logger
var winston = require('winston');

// promising using bluebird
var Promise = require('bluebird');

// discovery handler
var discoveryHandler = require('./discoveryhandler.js');


// mnemosyne configuration interface
var MnemosyneConfigurationInterface = require('../interfaces/mnemosyneconfiguration.js');

class ConfigurationhandlerClass {
    /**
     * Constructor for the class.
     * This will inititalise cache capabilities and set default values.
     * @constructor
     */
    constructor() {
        // this will contain a map of all cached configurations once they are loaded
        this.configurationCache = new Map();

        // mnemosyne configuration address and port
        // this will be filled by discovery
        this.mnemosyneConfigurationHostname = '';
        this.mnemosyneConfigurationPort = '';
    }

    /**
     * Discover the mnemosyne configuration service.
     * returns {Boolean} - Promised state.
     */
    discoverMnemosyneConfiguration() {
        return new Promise(function (resolve, reject) {
            // use discovery handler to get mnemosyne address and port
            discoveryHandler.getFirstMnemosyneConfigurationUrl(5000)
                .then(function (service) {
                    // iterate through found addresses
                    for (let i = 0; i < service.addresses.length; i++) {
                        // check if found address is a IPv4 one
                        if (service.addresses[i].indexOf('.') >= 0) {
                            configurationHandler.mnemosyneConfigurationHostname = service.addresses[i];
                            configurationHandler.mnemosyneConfigurationPort = service.port;
                        }
                    }

                    // if none was found, reject promise
                    if (configurationHandler.mnemosyneConfigurationHostname == '') {
                        reject('Could not find Mnemosyne Configuration service with proper IP');
                    }

                    // done
                    resolve(true);
                })
                .catch(function (error) {
                    reject(error);
                });
        });
    }

    /**
     * Load a device configuration from mnemosyne configuration.
     * @param {String} configurationId - The id of the device to load, as known to mnemosyne.
     * returns {Object} - Promised mnemosyne configuration object.
     */
    loadConfiguration(configurationID) {
        // check for configuration cache
        // if cached content is found in map, return it right away
        if (configurationHandler.configurationCache.has(configurationID)) {
            return new Promise(function (resolve, reject) {
                winston.debug('Returning cached configuration for ' + configurationID);
                resolve(configurationHandler.configurationCache.get(configurationID));
            });
        }

        // return the configuration object within a promise
        return new Promise(function (resolve, reject) {
            // create new digital signage player connection
            let mnemosyneConnection = new MnemosyneConfigurationInterface(configurationHandler.mnemosyneConfigurationHostname, configurationHandler.mnemosyneConfigurationPort);

            // get configuration via the mnemosyne configuration interface
            mnemosyneConnection.loadConfiguration(configurationID)
                .then(function (configurationResponse) {
                    // add the response to the cache
                    configurationHandler.configurationCache.set(configurationID, configurationResponse);

                    // done
                    resolve(configurationResponse);
                })
                .catch(function (error) {
                    reject(error);
                });
        });
    }

    /**
     * Get the connected devices based on the type.
     * Note: This requires the configuration to be already loaded and cached.
     * @param {String} configurationId - The id of the device to load, as known to mnemosyne.
     * @param {String} deviceType - The device type to look out for.
     * returns {Array} - Array of mnemosyne configuration objects.
     */
    getConnectedDevicesByType(configurationID, deviceType) {
        // check for configuration cache
        // if no cached content is found in map, return an empty object
        let configurationObject = this.configurationCache.get(configurationID);
        if (!configurationObject) {
            winston.debug('No cached configuration found for ' + configurationID);
            return [];
        }

        // storage for found devices
        let foundDevices = [];

        // iterate over all found connected devices, if any..
        if ((typeof configurationObject.location !== 'undefined') && (typeof configurationObject.location.devices !== 'undefined')) {
            for (let i = 0; i < configurationObject.location.devices.length; i++) {
                // here we assume that every device has a meta configuration node containing its type
                if (configurationObject.location.devices[i].meta.type == deviceType) {
                    // store found device configuration in object
                    foundDevices.push(configurationObject.location.devices[i]);
                }
            }
        }

        // return found devices
        return foundDevices;
    }

    /**
     * Get the configuration for the running server.
     * Note: It can be assumed that the configuration is always cached as this is the first configuration
     * the server loads on startup and won't run if it's not available.
     * @param {String} connectedDevice - A device id for a connected device in the configuration.
     * returns {Object} - Mnemosyne configuration object.
     */
    getServerConfiguration(connectedDevice) {
        let configurationObject = this.configurationCache.get('themis_content');
        if (!configurationObject) {
            winston.debug('No cached configuration found for themis_content');
            return false;
        }

        // if no device was requested, return everything
        if (!connectedDevice) {
            return configurationObject;
        }

        // else iterate over all found connected devices, to search for the requested one
        if ((typeof configurationObject.location !== 'undefined') && (typeof configurationObject.location.devices !== 'undefined')) {
            for (let i = 0; i < configurationObject.location.devices.length; i++) {
                // here we assume that every device has a meta configuration node containing its type
                if (configurationObject.location.devices[i].device == connectedDevice) {
                    // store found device configuration in object
                    return configurationObject.location.devices[i];
                }
            }
        }

        // if up to this point, nothing was found, return false
        return false;
    }
}

var configurationHandler = new ConfigurationhandlerClass();
module.exports = configurationHandler;