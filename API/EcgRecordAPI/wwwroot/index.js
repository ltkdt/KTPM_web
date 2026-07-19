(() => {
    const pageSize = 10;
    let currentPage = 1;
    let records = [];
    let detailChart = null;
    let isDemoMode = false;

    const tbody = document.querySelector('#recordsTable tbody');
    const detailContainer = document.getElementById('detailContainer');
    const patientId = localStorage.getItem('patientId');
    const patientName = localStorage.getItem('patientName');
    const statusLabels = {
        PENDING: '⏳ Đang chờ bác sĩ tư vấn',
        Pending: '⏳ Đang chờ bác sĩ tư vấn',
        RESPONDED: '✅ Bác sĩ đã phản hồi',
        Responded: '✅ Bác sĩ đã phản hồi',
        NOTCONSULTED: '📝 Chưa gửi yêu cầu tư vấn',
        NotConsulted: '📝 Chưa gửi yêu cầu tư vấn'
    };

    function statusText(status) {
        return statusLabels[status] ?? '📝 Chưa gửi yêu cầu tư vấn';
    }

    function render() {
        const totalPages = Math.max(1, Math.ceil(records.length / pageSize));
        currentPage = Math.min(currentPage, totalPages);
        document.getElementById('pageInfo').textContent = currentPage;
        document.getElementById('pageCount').textContent = totalPages;
        document.getElementById('totalRecordsCount').textContent = `(${records.length} bản ghi)`;
        document.getElementById('prevBtn').disabled = currentPage <= 1;
        document.getElementById('nextBtn').disabled = currentPage >= totalPages;

        tbody.querySelectorAll('tr:not(#detailContainer)').forEach(row => row.remove());
        const start = (currentPage - 1) * pageSize;
        records.slice(start, start + pageSize).forEach(record => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${record.id}</td>
                <td class="name-cell" data-id="${record.id}">
                    <strong>${record.name}</strong><br>
                    <small class="text-muted">${record.statusText}</small>
                </td>
                <td class="action-column"><button class="btn btn-sm btn-primary view-btn" data-id="${record.id}">Xem biểu đồ</button></td>`;
            tbody.insertBefore(row, detailContainer);
        });
    }

    function ensureDetailContent() {
        const card = detailContainer.querySelector('.detail-card');
        if (document.getElementById('consultationInfo')) return;
        const consultation = document.createElement('section');
        consultation.id = 'consultationInfo';
        consultation.className = 'mt-3 border-top pt-3';
        consultation.innerHTML = `
            <div class="row g-3">
                <div class="col-md-4"><label class="form-label small text-muted">Trạng thái tư vấn</label><div id="consultationStatus" class="fw-semibold"></div></div>
                <div class="col-md-8"><label class="form-label small text-muted">Bác sĩ phụ trách</label><div id="doctorName" class="fw-semibold"></div></div>
                <div class="col-12"><label class="form-label small text-muted">Triệu chứng</label><textarea id="complaintInput" class="form-control" rows="3"></textarea></div>
                <div class="col-md-6"><label class="form-label small text-muted">Nhận xét của bác sĩ</label><textarea id="findingsInput" class="form-control bg-light" rows="4" readonly></textarea></div>
                <div class="col-md-6"><label class="form-label small text-muted">Phác đồ điều trị</label><textarea id="treatmentInput" class="form-control bg-light" rows="4" readonly></textarea></div>
                <div class="col-12 text-end"><button id="sendComplaintBtn" class="btn btn-success">Gửi yêu cầu tư vấn</button></div>
            </div>`;
        card.appendChild(consultation);
    }

    async function loadSignal(record) {
        if (record.demo) return makeDemoSignal(record.variant);
        try {
            const response = await fetch(`/api/records/${record.id}/csv`);
            if (!response.ok) throw new Error('Không có tệp CSV');
            const text = await response.text();
            const values = text.split(/\r?\n/).slice(1).map(line => Number(line.split(',')[1])).filter(Number.isFinite);
            return values.length ? values.slice(0, 1000) : makeDemoSignal(record.variant);
        } catch {
            return makeDemoSignal(record.variant);
        }
    }

    function makeDemoSignal(variant = 0) {
        const values = [];
        const rate = 72 + variant * 8;
        for (let index = 0; index < 1000; index++) {
            const time = index / 250;
            const phase = (time * rate / 60) % 1;
            const p = 0.10 * Math.exp(-Math.pow((phase - .18) / .035, 2));
            const q = -0.13 * Math.exp(-Math.pow((phase - .39) / .012, 2));
            const r = 0.88 * Math.exp(-Math.pow((phase - .42) / .016, 2));
            const s = -0.25 * Math.exp(-Math.pow((phase - .46) / .02, 2));
            const t = 0.25 * Math.exp(-Math.pow((phase - .68) / .065, 2));
            values.push(p + q + r + s + t + .015 * Math.sin(index * .09));
        }
        return values;
    }

    function drawChart(values) {
        if (detailChart) detailChart.destroy();
        detailChart = new Chart(document.getElementById('detailChart'), {
            type: 'line',
            data: {
                labels: values.map((_, index) => (index / 250).toFixed(2)),
                datasets: [{ data: values, borderColor: '#0d6efd', borderWidth: 2, pointRadius: 0, tension: .08 }]
            },
            options: {
                responsive: true,
                plugins: { legend: { display: false } },
                scales: {
                    x: { title: { display: true, text: 'Thời gian (giây)' }, ticks: { maxTicksLimit: 9 } },
                    y: { title: { display: true, text: 'Điện áp (mV)' }, suggestedMin: -0.4, suggestedMax: 1.1 }
                }
            }
        });
    }

    async function showDetail(id) {
        const record = records.find(item => item.id === Number(id));
        if (!record) return;
        ensureDetailContent();
        const row = tbody.querySelector(`[data-id="${id}"]`)?.closest('tr');
        if (row) row.after(detailContainer);
        detailContainer.classList.remove('d-none');
        document.getElementById('detailMeta').textContent = record.name;
        document.getElementById('bpmValue').textContent = `${record.nhipTim ?? '--'} bpm`;
        document.getElementById('rmssdValue').textContent = record.rmssd == null ? '-- ms' : `${Number(record.rmssd).toFixed(1)} ms`;
        document.getElementById('consultationStatus').textContent = record.statusText;
        document.getElementById('doctorName').textContent = record.doctor ?? (record.status === 'RESPONDED' || record.status === 'Responded' ? 'Bác sĩ phụ trách' : 'Chưa phân công');
        document.getElementById('complaintInput').value = record.complaint ?? '';
        document.getElementById('findingsInput').value = record.findings ?? 'Chưa có nhận xét.';
        document.getElementById('treatmentInput').value = record.treatment ?? 'Chưa có phác đồ điều trị.';
        const sendButton = document.getElementById('sendComplaintBtn');
        sendButton.hidden = isDemoMode || !patientId || record.consultationId > 0;
        sendButton.onclick = () => sendComplaint(record, sendButton);
        drawChart(await loadSignal(record));
        detailContainer.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    async function sendComplaint(record, button) {
        const complaint = document.getElementById('complaintInput').value.trim();
        if (!complaint) return alert('Vui lòng nhập triệu chứng trước khi gửi.');
        button.disabled = true;
        button.textContent = 'Đang gửi...';
        try {
            const response = await fetch('/api/patient/complaint', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ PatientId: Number(patientId), EcgRecordId: record.id, Complaint: complaint }) });
            if (!response.ok) throw new Error();
            alert('Đã gửi yêu cầu tư vấn thành công.');
            await loadApiRecords();
            hideDetail();
        } catch {
            alert('Không thể gửi yêu cầu tư vấn. Vui lòng thử lại.');
        } finally {
            button.disabled = false;
            button.textContent = 'Gửi yêu cầu tư vấn';
        }
    }

    function hideDetail() {
        detailContainer.classList.add('d-none');
        if (detailChart) { detailChart.destroy(); detailChart = null; }
    }

    function loadDemoRecords() {
        isDemoMode = true;
        records = [
            { id: 901, name: 'Mẫu ECG bình thường', status: 'RESPONDED', statusText: '✅ Bác sĩ đã phản hồi', nhipTim: 72, rmssd: 38.6, complaint: 'Khám sức khỏe định kỳ, không có triệu chứng bất thường.', findings: 'Nhịp xoang đều, tín hiệu ECG trong giới hạn bình thường.', treatment: 'Duy trì vận động nhẹ và tái khám định kỳ.', doctor: 'BS. Nguyễn Minh An', consultationId: 901, demo: true, variant: 0 },
            { id: 902, name: 'Mẫu ECG cần theo dõi', status: 'PENDING', statusText: '⏳ Đang chờ bác sĩ tư vấn', nhipTim: 96, rmssd: 29.4, complaint: 'Cảm giác hồi hộp sau khi vận động mạnh.', findings: '', treatment: '', doctor: 'BS. Trần Thu Hà', consultationId: 902, demo: true, variant: 1 },
            { id: 903, name: 'Mẫu ECG nhịp nhanh', status: 'NOTCONSULTED', statusText: '📝 Chưa gửi yêu cầu tư vấn', nhipTim: 108, rmssd: 24.8, complaint: '', findings: '', treatment: '', doctor: '', consultationId: 0, demo: true, variant: 2 }
        ];
        currentPage = 1;
        render();
        showDetail(901);
    }

    async function loadApiRecords() {
        if (!patientId) return loadDemoRecords();
        try {
            const response = await fetch(`/api/records/${patientId}`);
            if (!response.ok) throw new Error();
            const items = await response.json();
            if (!items.length) return loadDemoRecords();
            isDemoMode = false;
            records = items.map(item => ({
                id: item.ecgId, name: `Bản ghi ECG #${item.ecgId}`, status: item.status,
                statusText: statusText(item.status), nhipTim: item.nhipTim, rmssd: item.rmssd,
                complaint: item.complaint, findings: item.findings, treatment: item.treatment,
                consultationId: item.consultationId ?? 0, demo: false, variant: item.ecgId % 3
            }));
            currentPage = 1;
            render();
        } catch {
            loadDemoRecords();
        }
    }

    document.getElementById('envDummy').textContent = patientName || 'Hồ sơ bệnh nhân';
    document.getElementById('demoBtn').addEventListener('click', loadDemoRecords);
    document.getElementById('logoutBtn').addEventListener('click', () => { localStorage.removeItem('patientId'); localStorage.removeItem('patientName'); window.location.href = '/login.html'; });
    document.getElementById('resetDbBtn').addEventListener('click', async () => {
        if (!confirm('Bạn có chắc muốn xóa toàn bộ ca tư vấn?')) return;
        const response = await fetch('/api/reset-database', { method: 'POST' });
        if (response.ok) { alert('Đã làm mới các ca tư vấn.'); loadApiRecords(); } else alert('Không thể làm mới dữ liệu tư vấn.');
    });
    document.getElementById('prevBtn').addEventListener('click', () => { if (currentPage > 1) { currentPage--; render(); } });
    document.getElementById('nextBtn').addEventListener('click', () => { if (currentPage * pageSize < records.length) { currentPage++; render(); } });
    tbody.addEventListener('click', event => { const target = event.target.closest('.view-btn, .name-cell'); if (target) showDetail(target.dataset.id); });
    document.getElementById('detailClose').addEventListener('click', hideDetail);
    if (typeof signalR !== 'undefined') {
        const connection = new signalR.HubConnectionBuilder().withUrl('/ecghub').withAutomaticReconnect().build();
        connection.on('DoctorSentFeedback', loadApiRecords);
        connection.on('NewRecordUploaded', loadApiRecords);
        connection.start().catch(() => {});
    }
    loadApiRecords();
})();
