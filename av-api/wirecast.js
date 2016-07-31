const Edge = require('edge'); //for .Net OLE WireCast API
const updateTimerLabel = 'Wirecast.update';
const setTimerLabel = 'Wirecast.set';
const refs = ['Newtonsoft.Json.dll','Nlog.dll'];
exports.wirecast = null;

const edgeFnErr = function(fnName) {
  return Edge.func({
      source: {
          assemblyFile: 'WireCastAsync.dll',
          typeName: 'WireCastAsync.Service',
          methodName: fnName // This must be Func<object,Task<object>>
      },
      references: refs
  });
};

const edgeFn = function(fnName) {
    return Edge.func({
        assemblyFile: 'WireCastAsync.dll',
        typeName: 'WireCastAsync.Service',
        methodName: fnName // This must be Func<object,Task<object>>
    });
};

const get = edgeFn('getWirecastAsync');
const set = edgeFn('setWirecastAsync');

exports.update = function() {
    //todo: do we need a lock to avoid this running and being called before it finishes
    console.time(updateTimerLabel);
    get('',function(error, result){
        if(error) throw error;
        exports.wirecast = result;
        console.timeEnd(updateTimerLabel);
    });
};

exports.set = function(input, cb) {
    console.time(setTimerLabel);
    console.log('body: '+JSON.stringify(input));
    set(input, function(error, result){
       if(error) cb(error);
        exports.wirecast = result;
        console.timeEnd(setTimerLabel);
        cb();
    });
};
