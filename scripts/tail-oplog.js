(function() {
    var op = db.oplog.rs.find().sort({ $natural: -1 }).limit(1).next();
    var ts = op.ts;
    for (; ;) {
        var commandResult = db.runCommand({
            find: "oplog.rs",
            filter: {
                "ts": { $gt: ts },
                "ns": {
                    $nin: [
                        "",
                        "config.system.sessions",
                        "demo.resumetokens", "demo.checkpoints",
                        "demo.events"
                    ]
                }
            },
            tailable: true,
            awaitData: true,
            oplogReplay: true
        });
        new DBCommandCursor(db, commandResult).forEach(d => {
            ts = d.ts;
            printjson(d);
        });
        sleep(100);
    }
})();