var vue;
var model;

function Page() {};

Page.getQuestionById = function(qId) {
    return _.find(model.data.questions, function(q) { return q.id == qId });
}

Page.init = function() {
    $(document).ready(function() {
        $.ajax({
                method: "POST",
                url: (location.origin + '/' + location.pathname + '/get-model').replace('interview', ''),
            }).done(function(res) {
                model = res;
                model.currentQuestion = Page.getQuestionById(model.currentQuestionId);
                sessionStorage.interviewId = model.interviewId;
                Page.bind();
                Page.set100vhContentSize();
            })
            .fail(function(jqXHR, textStatus, errorThrown) {
                console.log(jqXHR);
            });
    });
};

Page.postResult = function(aId, cb) {
    $.ajax({
            method: "POST",
            url: (location.origin + '/' + location.pathname + '/post-result').replace('interview', ''),
            data: {
                questionId: model.currentQuestion.id,
                answerId: aId,
                interviewId: sessionStorage.interviewId
            },
        }).done(function(res) {
            model.stats = res;
            if (cb) cb();
        })
        .fail(function(jqXHR, textStatus, errorThrown) {
            var a = 1;

        });
}

Page.bind = function() {
    model.stats = {};
    vue = new Vue({
        el: '.full-window-contrainer',
        data: model,
        methods: {
            selectAnswer: function(answer) {
                $('.answer-stat').removeClass('undisp');
                if (!answer || model.currentQuestion.checked)
                    return;

                answer.checked = true;

                if (answer.characterIdPoints != '') {
                    var aCharacters = answer.characterIdPoints.split(',');

                    for (var i = 0; i < aCharacters.length; i++) {
                        model.data.characterPoints[aCharacters[i]].points++;
                    }
                }

                var cb = function() {
                    model.currentQuestion.checked = true;
                };

                Page.postResult(answer.id, cb);
            },
            getAnswerProgressWidth: function(aId) {
                if (!model.currentQuestion.checked) return '0px';
                var res = model.stats[aId] * 0.85;
                return res + '%';
            },
            getTotalCountInterviewsEnding: function() {
                if (model.statTotalInterviewCount == 1)
                    return "s";
                return "";
            },
            nextQuestion: function() {
                $('.interview-container').addClass('hidden');

                setTimeout(function() {
                    $('.answer-stat').addClass('undisp');
                }, 300);

                //if current question is last
                if (model.currentQuestion.id == data.questions[data.questions.lenght - 1].id) {
                    $.ajax({
                            method: "POST",
                            url: (location.origin + '/' + location.pathname + '/post-characters').replace('interview', ''),
                            data: { characterPoints: (model && model.data ? model.data.characterPoints : null), isMan: model.currentQuestion.answers[0].checked, interviewId: sessionStorage.interviewId ? sessionStorage.interviewId : 0 },
                        }).done(function(res) {
                            location.href = (location.origin + '/' + location.pathname).replace('interview', '') + '/result?charId=' + res;
                        })
                        .fail(function(jqXHR, textStatus, errorThrown) {
                            location.href = (location.origin + '/' + location.pathname).replace('interview', '') + '/result?charId=1';
                        });
                }

                setTimeout(function() {
                    //beauty next question
                    model.currentQuestion = Page.getQuestionById(qId + 1);
                    $('.interview-container').removeClass('hidden');
                }, 700);


            }
        },
        mounted: function() {
            this.$nextTick(function() {
                //beauty showing
                setTimeout(function() {
                    $('.interview-container').removeClass('hidden');
                }, 50)
            })
        }
    });


};

Page.init();