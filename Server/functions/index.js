const functions = require('firebase-functions');
const admin = require('firebase-admin');

admin.initializeApp();

exports.playerJoinedLobby = functions.database.ref('/lobbies/{id}/users/{uid}/user-id').onCreate((snapshot, context) => {

    const getLobbyUserIds = "0123".split('')
        .filter(x => x !== context.params.uid)
        .map(x => admin.database().ref(`/lobbies/${context.params.id}/users/${x}/user-id`).once('value'));

    return Promise.all(getLobbyUserIds).then(results => {

        const getUserNotificationTokens = results
            .map(x => admin.database().ref(`/users/${x.val()}/notification-token`).once('value'));

        return Promise.all(getUserNotificationTokens);

    }).then(results => {

        const notificationTokens = results
            .map(x => x.val())
            .filter(x => !!x);

        const payload = {
            notification: {
                title: 'A player has joined the game!',
                body: 'A player has joined the game!'
            }
        };

        return admin.messaging().sendToDevice(notificationTokens, payload);

    });

});

exports.playerLeftLobby = functions.database.ref('/lobbies/{id}/users/{uid}/user-id').onDelete((snapshot, context) => {

    const getLobbyUserIds = "0123".split('')
        .filter(x => x !== context.params.uid)
        .map(x => admin.database().ref(`/lobbies/${context.params.id}/users/${x}/user-id`).once('value'));

    return Promise.all(getLobbyUserIds).then(results => {

        const getUserNotificationTokens = results
            .map(x => admin.database().ref(`/users/${x.val()}/notification-token`).once('value'));

        return Promise.all(getUserNotificationTokens);

    }).then(results => {

        const notificationTokens = results
            .map(x => x.val())
            .filter(x => !!x);

        const payload = {
            notification: {
                title: 'A player has left the game!',
                body: 'A player has left the game!'
            }
        };

        return admin.messaging().sendToDevice(notificationTokens, payload);

    });

});

exports.lobbyStarted = functions.database.ref('/lobbies/{id}/state').onUpdate((snapshot, context) => {

    const getLobbyUserIds = "0123".split('')
        .map(x => admin.database().ref(`/lobbies/${context.params.id}/users/${x}/user-id`).once('value'));

    return Promise.all(getLobbyUserIds).then(results => {

        const getUserNotificationTokens = results
            .map(x => admin.database().ref(`/users/${x.val()}/notification-token`).once('value'));

        return Promise.all(getUserNotificationTokens);

    }).then(results => {

        const notificationTokens = results
            .map(x => x.val())
            .filter(x => !!x);

        const payload = {
            notification: {
                title: 'The game has started!',
                body: 'The game has started!'
            }
        };

        return admin.messaging().sendToDevice(notificationTokens, payload);

    });

});

exports.clueChanged = functions.database.ref('/lobbies/{id}/users/{uid}/items/{iid}/description').onWrite((snapshot, context) => {

    const getLobbyUserIds = "0123".split('')
        .filter(x => x !== context.params.uid)
        .map(x => admin.database().ref(`/lobbies/${context.params.id}/users/${x}/user-id`).once('value'));

    return Promise.all(getLobbyUserIds).then(results => {

        const getUserNotificationTokens = results
            .map(x => admin.database().ref(`/users/${x.val()}/notification-token`).once('value'));

        return Promise.all(getUserNotificationTokens);

    }).then(results => {

        const notificationTokens = results
            .map(x => x.val())
            .filter(x => !!x);

        const payload = {
            notification: {
                title: 'New items have been added to the database!',
                body: 'New items have been added to the database!'
            }
        };

        return admin.messaging().sendToDevice(notificationTokens, payload);

    });

});

exports.playerReady = functions.database.ref('/lobbies/{id}/users/{uid}/ready').onCreate((snapshot, context) => {

    const getLobbyUserIds = "0123".split('')
        .filter(x => x !== context.params.uid)
        .map(x => admin.database().ref(`/lobbies/${context.params.id}/users/${x}/user-id`).once('value'));

    return Promise.all(getLobbyUserIds).then(results => {

        const getUserNotificationTokens = results
            .map(x => admin.database().ref(`/users/${x.val()}/notification-token`).once('value'));

        return Promise.all(getUserNotificationTokens);

    }).then(results => {

        const notificationTokens = results
            .map(x => x.val())
            .filter(x => !!x);

        const payload = {
            notification: {
                title: 'A player is ready to vote!',
                body: 'A player is ready to vote!'
            }
        };

        return admin.messaging().sendToDevice(notificationTokens, payload);

    });
    
});

exports.clueHighlighted = functions.database.ref('/lobbies/{id}/users/{uid}/items/{iid}/highlight').onCreate((snapshot, context) => {

    const getLobbyUserIds = "0123".split('')
        .map(x => admin.database().ref(`/lobbies/${context.params.id}/users/${x}/user-id`).once('value'));

    return Promise.all(getLobbyUserIds).then(results => {

        const getUserNotificationTokens = results
            .map(x => admin.database().ref(`/users/${x.val()}/notification-token`).once('value'));

        return Promise.all(getUserNotificationTokens);

    }).then(results => {

        const notificationTokens = results
            .map(x => x.val())
            .filter(x => !!x);

        const payload = {
            notification: {
                title: 'New items have been highlighted in the database!',
                body: 'New items have been highlighted in the database!'
            }
        };

        return admin.messaging().sendToDevice(notificationTokens, payload);

    });

});

exports.playerVoted = functions.database.ref('/lobbies/{id}/users/{uid}/vote').onCreate((snapshot, context) => {

    const getLobbyUserIds = "0123".split('')
        .filter(x => x !== context.params.uid)
        .map(x => admin.database().ref(`/lobbies/${context.params.id}/users/${x}/user-id`).once('value'));

    return Promise.all(getLobbyUserIds).then(results => {

        const getUserNotificationTokens = results
            .map(x => admin.database().ref(`/users/${x.val()}/notification-token`).once('value'));

        return Promise.all(getUserNotificationTokens);

    }).then(results => {

        const notificationTokens = results
            .map(x => x.val())
            .filter(x => !!x);

        const payload = {
            notification: {
                title: 'A player has voted!',
                body: 'A player has voted!'
            }
        };

        return admin.messaging().sendToDevice(notificationTokens, payload);

    });
    
});

exports.playerRetry = functions.database.ref('/lobbies/{id}/users/{uid}/retry').onCreate((snapshot, context) => {

    const getLobbyUserIds = "0123".split('')
        .filter(x => x !== context.params.uid)
        .map(x => admin.database().ref(`/lobbies/${context.params.id}/users/${x}/user-id`).once('value'));

    return Promise.all(getLobbyUserIds).then(results => {

        const getUserNotificationTokens = results
            .map(x => admin.database().ref(`/users/${x.val()}/notification-token`).once('value'));

        return Promise.all(getUserNotificationTokens);

    }).then(results => {

        const notificationTokens = results
            .map(x => x.val())
            .filter(x => !!x);

        const payload = {
            notification: {
                title: 'A player wants to retry!',
                body: 'A player wants to retry!'
            }
        };

        return admin.messaging().sendToDevice(notificationTokens, payload);

    });
    
});