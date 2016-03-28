// *************************************************** //
// Razorshophandler class
//
// This script takes care incoming Razorshop requests
// and merge them into one file.
// *************************************************** //

// file system
var fileSystem = require('fs');
var filePath = require('path');

// http module
var http = require('http');

// log handler
var logHandler = require('./loghandler.js');

// configuration handler
var configurationHandler = require('./configurationhandler.js');

// merge
var merge = require('merge');

class RazorhandlerClass {
	constuctor() {
		// do nothing
	}

	getItemFromLocalFile(requestedConfigurationID, items) {
		let itemID, returnObj;
		let tempArray = requestedConfigurationID.split("/");
		if (tempArray.length > 2) {
			itemID = tempArray.pop().split(".")[0];
			requestedConfigurationID = tempArray.join("/");
			logHandler.log('Requested Item : ' + itemID, 3);
		}

		let configurationSourcePath = '/../data/' + requestedConfigurationID + '.json';
		logHandler.log('Loading ' + configurationSourcePath, 0);

		let mainConfiguration = configurationHandler.loadLocalDataFile(configurationSourcePath);

		if (typeof mainConfiguration[items] !== 'undefined')
		{
			for (var i = mainConfiguration[items].length - 1; i >= 0; i--)
			{
				if (mainConfiguration[items][i].id === Number(itemID) || mainConfiguration[items][i].id === itemID)
				{
					returnObj = mainConfiguration[items][i];
				}
			}
		}
		return returnObj;
	}

	executeRazorshopStyles(requestedConfigurationID)
	{
		let styleObj = this.getItemFromLocalFile(requestedConfigurationID, 'styles')
		let productID, productObj;
		if (typeof styleObj !== 'undefined' && typeof styleObj.products !== 'undefined')
		{
			logHandler.log('Products : ' + styleObj.products.length, 0);
			for (var i = styleObj.products.length - 1; i >= 0; i--)
			{
				let productID = styleObj.products[i];
				logHandler.log('Product : ' + styleObj.products[i], 0);

				// let productObj = this.getItemFromLocalFile('/razorshop/products/' + productID, 'products');
				let productObj = this.executeRazorshopProducts('/razorshop/products/' + productID);
				logHandler.log('Product Response : ' + productObj, 0);
				styleObj.products[i] = merge(styleObj.products[i], productObj);
			}
		}
		/*
		else
		{
			let msg = "no style found for request : " + requestedConfigurationID;
			styleObj = { error: msg };
		}
		*/
		return styleObj;
	}

	executeRazorshopProducts(requestedConfigurationID)
	{
		let productObj = this.getItemFromLocalFile(requestedConfigurationID, 'products')
		let colorID, colorObj;

		if (typeof productObj !== 'undefined' && typeof productObj.huecolor !== 'undefined')
		{
			logHandler.log('Color ID : ' + productObj.huecolor, 0);
			let colorID = productObj.huecolor;

			// let colorObj = this.getItemFromLocalFile('/razorshop/products/' + productID, 'products');
			let colorObj = this.executeRazorshopColors('/razorshop/colors/' + colorID);
			logHandler.log('Color Response : ' + colorObj, 0);
			productObj.huecolor = merge(productObj.huecolor, colorObj);
		}
		/*
		else
		{
			let msg = "no product found for request : " + requestedConfigurationID;
			productObj = { error: msg };
		}
		*/
		return productObj;
	}

	executeRazorshopColors(requestedConfigurationID)
	{
		let colorObj = this.getItemFromLocalFile(requestedConfigurationID, 'colors');
		return colorObj;
	}

	executeRazorshopItems(requestedConfigurationID)
	{
		let configurationSourcePath = '/../data/' + requestedConfigurationID + '.json';

		logHandler.log('Loading ' + configurationSourcePath, 0);

		let mainConfiguration = configurationHandler.loadLocalDataFile(configurationSourcePath);

		if (typeof mainConfiguration !== 'undefined' && typeof mainConfiguration.items !== 'undefined')
		{
			for (var i = mainConfiguration.items.length - 1; i >= 0; i--)
			{
				if (typeof mainConfiguration.items[i].huecolor !== 'undefined')
				{
					let colorID = mainConfiguration.items[i].huecolor;
					logHandler.log('Item : ' + mainConfiguration.items[i].name + ' / Color : ' + colorID, 0);

					let colorObj = this.executeRazorshopColors('/razorshop/colors/' + colorID);
					logHandler.log('Color Response : ' + colorID, 0);
					mainConfiguration.items[i].huecolor = merge(mainConfiguration.items[i].huecolor, colorObj);
				}
			}
		}
		/*
		else
		{
			let msg = "no item found for request : " + requestedConfigurationID;
			mainConfiguration = { error: msg };
		}
		*/
		return JSON.stringify(mainConfiguration);
	}

	executeRazorshopUsers(requestedConfigurationID) {
		requestedConfigurationID = requestedConfigurationID.replace(/\/+$/g, '');
		let configurationSourcePath = '/../data/' + requestedConfigurationID + '.json';

		logHandler.log('Loading ' + configurationSourcePath, 0);

		let mainConfiguration = configurationHandler.loadLocalDataFile(configurationSourcePath);

		if (typeof mainConfiguration.users !== 'undefined')
		{
			for (var i = mainConfiguration.users.length - 1; i >= 0; i--)
			{
				if (typeof mainConfiguration.users[i].styles !== 'undefined')
				{
					for (var j = mainConfiguration.users[i].styles.length - 1; j >= 0; j--)
					{
						let styleID = mainConfiguration.users[i].styles[j].id;
						logHandler.log('User : ' + mainConfiguration.users[i].name + ' / Style : ' + styleID, 0);

						let styleObj = this.executeRazorshopStyles('/razorshop/styles/' + styleID);
				        logHandler.log('Style Response : ' + styleObj, 0);
				        mainConfiguration.users[i].styles[j] = merge(mainConfiguration.users[i].styles[j], styleObj);
					}
				}
				if (typeof mainConfiguration.users[i].huecolor !== 'undefined')
				{
					let colorID = mainConfiguration.users[i].huecolor;
					logHandler.log('User : ' + mainConfiguration.users[i].name + ' / Color : ' + colorID, 0);

					let colorObj = this.executeRazorshopColors('/razorshop/colors/' + colorID);
					logHandler.log('Color Response : ' + colorID, 0);
					mainConfiguration.users[i].huecolor = merge(mainConfiguration.users[i].huecolor, colorObj);
				}
			}
		}
		return JSON.stringify(mainConfiguration);
	}
}

var razorshopHandler = new RazorhandlerClass();
module.exports = razorshopHandler;
