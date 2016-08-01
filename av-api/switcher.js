/**
 * Created by dev-lpalnau on 7/30/2016.
 */
const Atem = require('applest-atem'); //for ATEM TV Studio control
const Nconf = require('nconf');

exports.init = function(cb, stateChanged) {
    Nconf.argv().env().file({file:'config.json'});
    var switcherIp = Nconf.get('SWITCHER_IP');
    console.log('connecting to ATEM switcher at '+switcherIp);
    var switcher = new Atem();
    switcher.connect(switcherIp);

    switcher.on('stateChange', function(error, state) {
        if(error) {
            console.log('switcher error: ' +error);
            stateChanged(error,null);
        }
        else {
            console.log('switcher state changed: ', JSON.stringify(state));
            stateChanged(null,state);
        }
    });

    switcher.on('connect', function(){
        console.log('switcher connected at '+switcherIp);
        console.log('switcher state: '+ JSON.stringify(switcher.state));
        cb(null,switcher.state);
    });
    exports.switcher = switcher;
};

exports.setPreview = function(id) {
    if(id<0) return; //todo: undefined check
    exports.switcher.changePreviewInput(id);
    exports.switcher.autoTransition();
};

exports.set = function(state, cb) {
    //todo: check for changes
    cb(null,exports.switcher.state);
};



