var player,
    time_update_interval = 0;

var ready = false;

function onYouTubeIframeAPIReady() {
    ready = true;
}

function onYouTubeIframeAPIReady2() {
    if (ready) {

        player = new YT.Player('video-placeholder', {
            width: 600,
            height: 400,
            videoId: '7GElP4YdrBE',
            playerVars: {
                color: 'white',
                playlist: 'G9kz-tag04U'
            },
            events: {

            }
        });
    }
}

function play() {
    player.playVideo();
}
function pause() {
    player.pauseVideo();
}


// Helper Functions

function formatTime(time){
    time = Math.round(time);

    var minutes = Math.floor(time / 60),
        seconds = time - minutes * 60;

    seconds = seconds < 10 ? '0' + seconds : seconds;

    return minutes + ":" + seconds;
}


$('pre code').each(function(i, block) {
    hljs.highlightBlock(block);
});