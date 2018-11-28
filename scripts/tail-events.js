(function tailEvents() {
    var event = db.events.find().sort({timestamp: -1}).limit(1).next();
    var ts = event.timestamp;
    for(;;) {
        db.events.find({timestamp: {$gt: ts}}).forEach(e => {
            ts = e.timestamp;
            printjson(e);
        });
    }
})();