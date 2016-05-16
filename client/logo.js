(function () {

    var canvas = document.getElementById("logo-canvas");
    var width = canvas.width;
    var height = canvas.height;
    var ctx = canvas.getContext("2d");
    ctx.font = "60px Arial";
    var gradient = ctx.createLinearGradient(0, 0, canvas.width, 0);
    gradient.addColorStop("1", "white");
    ctx.fillStyle = gradient;
    ctx.fillText("Chatty McChatface", 0, 50);

    var runShiz = function () {

        ctx.globalAlpha = 0.95;
        ctx.globalCompositeOperation = "lighter";
        ctx.lineWidth = 1.5;
        ctx.strokeStyle = "#ff5522";

        doStyle();
    }
    
    function doStyle(){
        var time = Date.now() / 1600;
        var mix = Math.sin(time);
        ctx.translate(width / 2, height / 2);
        ctx.rotate(Date.now() / 1600 % (Math.PI * 2) * (1 - mix));
        ctx.beginPath();
        for (var xy, t = 0; t < Math.PI * 12; t += Math.PI / 32) {
            xy = mixf(f1(t), f2(t), mix);
            ctx.lineTo(xy.x * 8, -xy.y * 8);
        }
        ctx.closePath();
        ctx.stroke();
    }

    function mixf(xy1, xy2, mix) {
        return {
            x: xy1.x * mix + xy2.x * (1 - mix),
            y: xy1.y * mix + xy2.y * (1 - mix)
        };
    }

    function f1(t) {
        return {
            x: 13 * Math.cos(t) - 6 * Math.cos(11 / 6 * t),
            y: 11 * Math.sin(t) - 6 * Math.sin(11 / 6 * t)
        };
    }

    function f2(t) {
        return {
            x: 3 * (Math.cos(3 * t)),
            y: 5 * (3 * Math.sin(t))
        };
    }

    function animateFrame() {
        ctx.save();
        runShiz();
        ctx.restore();
        window.requestAnimationFrame(animateFrame)
    }


    runShiz();
    window.requestAnimationFrame(animateFrame)

})();