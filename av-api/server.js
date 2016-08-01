/**
 * Created by dev-lpalnau on 7/29/2016.
 */
'use strict';

const Hapi = require('hapi'); //for REST api endpoints
const Good = require('good');

const Wirecast = require('./wirecast.js');
const Camera = require('./camera.js');
const Switcher = require('./switcher.js');

// Create a server with a host and port
const server = new Hapi.Server();
server.connection({
    host: 'localhost',
    port: 8000
});

server.route({
    method: 'GET',
    path: '/switcher',
    handler: function (request, reply) {
        return reply(Switcher.switcher.state);
    }
});

server.route({
    method: 'POST',
    path: '/switcher',
    handler: function (request, reply) {
        Switcher.set(request.payload, function(error){
            if(error) throw error;
            return reply(Switcher.switcher.state);
        });
    }
});

server.route({
    method: 'GET',
    path: '/camera',
    handler: function (request, reply) {
        var cameras = [
            {ip:'192.168.0.1',p:0,t:10,z:5},
            {ip:'192.168.0.2',p:20,t:50,z:15},
        ];
        return reply(cameras);
    }
});

server.route({
    method: 'GET',
    path: '/stream',
    handler: function (request, reply) {
        return reply(Wirecast.wirecast);
    }
});

server.route({
    method: 'POST',
    path: '/stream',
    handler: function (request, reply) {
        Wirecast.set(request.payload, function(error){
            if(error) throw error;
            return reply(Wirecast.wirecast);
        });
    }
});

server.register({
    register: Good,
    options: {
        reporters: {
            console: [{
                module: 'good-squeeze',
                name: 'Squeeze',
                args: [{
                    response: '*',
                    log: '*'
                }]
            }, {
                module: 'good-console'
            }, 'stdout']
        }
    }
}, (error) => {
    if (error) throw error; // something bad happened loading the plugin
});

// Start the server
server.start((error) => {
    if (error) throw error;
    setInterval(Wirecast.update, 2000);
    Wirecast.update();
    Switcher.init(function(error,state){},function(error,state){});
    console.log('Server running at:', server.info.uri);
});

