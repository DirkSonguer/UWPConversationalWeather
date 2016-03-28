// *************************************************** //
// Mock service for Open Weather Maps
// *************************************************** //

// add express server
var express = require('express');
var app = express();

// setup cors
app.use(function (req, res, next) {
    res.header("Access-Control-Allow-Origin", "*");
    res.header("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
    next();
});

// initialise server
function init() {
    // add static routes for assets
    app.use(express.static('assets'));

    app.get('/', function (req, res) {
        res.send('Request for document main received. Server is working.');
    });

    var server = app.listen("8888", function () {
        console.log('Themis Content Storage listening..');
    });

}

init();