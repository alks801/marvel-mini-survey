sessionStorage.interviewId = -1;
$('.fb-share').on('click', function (e) {
    FB.ui({
        method: 'share',
        display: 'popup',
        href: 'my-site=' + charId,
    }, function (response) {
        console.log(response);
    });
});

$('.new-interview').on('click', function (e) {
    location.href = location.origin;
})