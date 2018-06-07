/*! Bleep
* Copyright Wheatley Park School */

var active = false;
var timer;

function checkTime() {
  var date = new Date();
  var day = date.getDay();
  var hours = date.getHours();
  var mins = date.getMinutes();
  var show = (day > 0 && day < 6 && (hours > 8 || (hours == 8 && mins >= 15)) && (hours < 15 || (hours == 15 && mins <= 10)));
  $('#in-hours').toggle(show);
  $('#out-of-hours').toggle(!show);
}

function makeRequest(priority) {
  var student = $('#student').val();
  if (student == '') {
    $('#error').text('Error: Student name required');
    $('#student').focus();
    return;
  }
  var room = $('#room').val();
  if (room == '') {
    $('#error').text('Error: Room number required');
    $('#room').focus();
    return;
  }
  if (room.length <= 3 && (!isNaN(parseInt(room.charAt(1), 10)) || !isNaN(parseInt(room.charAt(2), 10)))) {
    if (room.charAt(0) == '0') {
      room = 'O' + room.substr(1);
    }
    room = room.toUpperCase();
  } else {
    room = room.charAt(0).toUpperCase() + room.substr(1);
  }
  $('#room').val(room);
  localStorage.setItem('room', room);
  $('#error').text('');
  if (priority == 1) {
    $('.request-button[data-priority=1]').removeClass('btn-outline-danger').addClass('btn-danger');
  } else {
    $('.request-button[data-priority=2]').removeClass('btn-outline-secondary').addClass('btn-secondary');
  }
  $('.request-button,input').prop('disabled', 'disabled');
  $('#progress').css('opacity', 1);
  if (priority == 1) {
    $('#telephoning').show();
  }
  active = true;

  $.post('/post', { studentName: student, room: room, priority: priority, __RequestVerificationToken: $('input[name=__RequestVerificationToken]').val() })
    .fail(function (e) { $('#progress').hide(); $('#error').text('Failed: ' + e.responseText); active = false; clearInterval(timer); });
}

function match(request, response) {
  var parts = request.term.split(' ', 2);
  var matchers = [];
  for (var i = 0; i < parts.length; i++) {
    matchers[i] = new RegExp('\\b' + parts[i], "i");
  }
  response($.grep(students, function (item) {
    for (var i = 0; i < matchers.length; i++) {
      if (!matchers[i].test(item)) return false;
    }
    return true;
  }).slice(0, 15));
}

var connection = new signalR.HubConnectionBuilder().withUrl("/hub").configureLogging(signalR.LogLevel.Information).build();

connection.on('UpdateClient', (command, data) => {
  if (!active) {
    return;
  }
  switch (command) {
    case 'emailSent':
      $('#email-checkbox').text('check_box');
      $('#email-ellipsis').remove();
      if (data == 2) {
        $('#done').show();
        active = false;
        clearInterval(timer);
      }
      break;
    case 'phoneStart':
      $('.ellipsis').remove();
      $('.phone').addClass('old');
      $('.phone:not(.delay)>i').text('phone_missed');
      $('#telephoning').append($('<div />').addClass('phone').append($('<i />').addClass('material-icons').text('phone_in_talk')).append(' ' + data).append($('<span/>').addClass('ellipsis').text('...')));
      break;
    case 'phoneDelay':
      $('.ellipsis').remove();
      $('.phone').addClass('old');
      $('.phone:not(.delay)>i').text('phone_missed');
      $('#telephoning').append($('<div />').addClass('phone delay').append($('<i />').addClass('material-icons').text('schedule')).append(' Waiting ' + data + ' sec for retry').append($('<span/>').addClass('ellipsis').text('...')));
      break;
    case 'phoneDone':
      $('#phone-checkbox').text('check_box');
      $('#phone-ellipsis,.ellipsis').remove();
      $('.phone:last-child>i').text('done');
      $('#done').show();
      active = false;
      clearInterval(timer);
      break;
    case 'phoneFail':
      $('#phone-checkbox').text('error_outline');
      $('#phone-ellipsis,.ellipsis').remove();
      $('.phone').addClass('old');
      $('.phone:not(.delay)>i').text('phone_missed');
      $('#fail').show();
      active = false;
      clearInterval(timer);
      break;
  }
});

connection.start();

$(function () {
  checkTime();

  $('#room').val(localStorage.getItem('room'));

  $('#student').autocomplete({
    source: match, delay: 0
  }).focus();

  $('.request-button').click(function () {
    makeRequest($(this).data('priority'));
  });

  timer = setInterval(checkTime, 60000);
});
