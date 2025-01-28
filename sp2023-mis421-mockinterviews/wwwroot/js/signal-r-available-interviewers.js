$(document).ready(() => {
    const connection = new signalR.HubConnectionBuilder().withUrl("/interviewershub").build();

    connection.start().catch(err => console.error(err.toString()));

    connection.on("ReceiveAvailableInterviewersUpdate", (interviewers) => {
        console.log('Received available interviewers:', interviewers);
        const $selectBox = $('#InterviewerId');

        if ($selectBox.children('option').length !== 0) {
            const id = $('#Id').val();
            const type = $('#Type').val();
            const status = $('#Status').val();
            populateInterviewerSelectList(id, type, status);
        }

        const $availableInterviewersView = $('#availableInterviewers');
        $availableInterviewersView.empty();

        interviewers.forEach(interviewer => {
            const $newListItem = createInterviewerListItem(interviewer);
            $availableInterviewersView.append($newListItem);
        });
    });

    // Helper function to create a list item for an interviewer
    const createInterviewerListItem = (interviewer) => {
        const $listItem = $('<li></li>').attr('id', interviewer.id).css('color', 'black');

        const $name = $('<b></b>')
            .append($('<u></u>').text(interviewer.name));

        const $type = $('<b></b>')
            .append($('<u></u>').text(interviewer.interviewType));

        const $room = $('<b></b>')
            .append($('<u></u>').text(interviewer.room));

        $listItem.append($name)
            .append(' is available to do ')
            .append($type)
            .append(' interviews in room ')
            .append($room)
            .append('.');

        return $listItem;
    }
});
