const editInfo = document.querySelector('.edit-info')
const editElement = (elementId) => {
    const input = document.getElementById(`${elementId}`);
    fetch(`/Home/EditElement?value=${input.value}&id=${elementId}`)
        .then(response => response.text())
        .then(data => {
            editInfo.innerText = data;
        })
}

const editAttribute = (elementId) => {
    const input = document.getElementById(`${elementId}`);
    fetch(`/Home/EditAttribute?value=${input.value}&id=${elementId}`)
        .then(response => response.text())
        .then(data => {
            editInfo.innerText = data;
        })
}