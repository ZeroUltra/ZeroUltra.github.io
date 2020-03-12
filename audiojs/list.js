$(function() {
    var ar=new Array(2)
ar[0]="/mp3/1.mp3"
ar[1]="/mp3/2.mp3"
var x = 0;
    var a = audiojs.createAll({
        trackEnded: function() {
			x+=1;
            var next = ar[x];
            if (x>ar.length) x=0;
            next.addClass('playing').siblings().removeClass('playing');
            audio.load(next));
            audio.play();
        }
    });



});