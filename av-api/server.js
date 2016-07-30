/**
 * Created by dev-lpalnau on 7/29/2016.
 */
'use strict';

const Hapi = require('hapi'); //for REST api endpoints
const Atem = require('atem'); //for ATEM TV Studio control
const IPCamController = require('ipcam-controller'); //for PTZOptics camera controls
const Edge = require('edge'); //for .Net OLE WireCast API

// Create a server with a host and port
const server = new Hapi.Server();
server.connection({
    host: 'localhost',
    port: 8000
});

var helloWorld = Edge.func(function () {/*
 async (input) => {
    return ".NET Welcomes " + input.ToString();
 }
*/});

// Add the route
server.route({
    method: 'GET',
    path: '/hello',
    handler: function (request, reply) {

        helloWorld('JavaScript', function (error, result) {
            if (error) throw error;
            return reply({message:'hello world '+result});
        });
    }
});

server.route({
    method: 'GET',
    path: '/switcher',
    handler: function (request, reply) {
        return reply({preview:1,program:2});
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
        return reply({on:true,health:.5,cpu:.2});
    }
});

server.route({
    method: 'GET',
    path: '/recording',
    handler: function (request, reply) {
        return reply({on:false});
    }
})

// Start the server
server.start((err) => {

    if (err) {
        throw err;
    }
    console.log('Server running at:', server.info.uri);
});

