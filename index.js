// Sample renderer for the records table with paging and a Time column
(function(){
	const perPage = 10;
	let currentPage = 1;

	// generate sample data
	const records = Array.from({length:15}, (_,i)=>{
		const id = i+1;
		const pad = n=>String(n).padStart(2,'0');
		const hour = 8 + (i % 10);
		const minute = 5 + (i*3)%55;
		const time = `2026-03-30 ${pad(hour)}:${pad(minute)}`;
		return {id, name:`ecg_record_${id}.json`, time};
	});

	const tbody = document.querySelector('#recordsTable tbody');
	const prevBtn = document.getElementById('prevBtn');
	const nextBtn = document.getElementById('nextBtn');
	const pageInfo = document.getElementById('pageInfo');
	const pageCount = document.getElementById('pageCount');

	function render(){
		const totalPages = Math.max(1, Math.ceil(records.length / perPage));
		pageCount.textContent = totalPages;
		pageInfo.textContent = currentPage;

		const start = (currentPage-1)*perPage;
		const slice = records.slice(start, start+perPage);

		tbody.innerHTML = '';
		slice.forEach(r=>{
			const tr = document.createElement('tr');
			tr.innerHTML = `
				<td>${r.id}</td>
				<td class="name-cell" data-id="${r.id}">${r.name} <small class="text-muted ms-2">${r.time}</small></td>
				<td class="action-column"><button class="btn btn-sm btn-primary view-btn" data-id="${r.id}">View</button></td>
			`;
			tr.addEventListener('click', ()=>{
				document.querySelectorAll('#recordsTable tbody tr').forEach(x=>x.classList.remove('selected'));
				tr.classList.add('selected');
			});
			tbody.appendChild(tr);
		});

		prevBtn.disabled = currentPage <= 1;
		nextBtn.disabled = currentPage >= totalPages;
	}

	prevBtn.addEventListener('click', ()=>{
		if(currentPage>1){ currentPage--; render(); }
	});
	nextBtn.addEventListener('click', ()=>{
		const totalPages = Math.max(1, Math.ceil(records.length / perPage));
		if(currentPage<totalPages){ currentPage++; render(); }
	});

	// env toggle buttons (envProd may not exist in the DOM; guard it)
	const envDummy = document.getElementById('envDummy');
	const envProd = document.getElementById('envProd');
	function setEnv(dummy){
		if(dummy){
			envDummy.classList.add('btn-primary'); envDummy.classList.remove('btn-light');
			if(envProd){ envProd.classList.remove('btn-primary'); envProd.classList.add('btn-light'); }
		} else {
			if(envProd){ envProd.classList.add('btn-primary'); envProd.classList.remove('btn-light'); }
			envDummy.classList.remove('btn-primary'); envDummy.classList.add('btn-light');
		}
	}
	envDummy.addEventListener('click', ()=> setEnv(true));
	if(envProd) envProd.addEventListener('click', ()=> setEnv(false));

	// initial state
	setEnv(true);
	render();

	// Detail panel logic (create lazily because render() recreates tbody)
	let detailChart = null;
	let detailContainer = document.getElementById('detailContainer');

	function ensureDetailElements(){
		// if the container isn't in the DOM, create it
		if(!detailContainer){
			detailContainer = document.createElement('tr');
			detailContainer.id = 'detailContainer';
			detailContainer.className = 'd-none';
			detailContainer.innerHTML = `
				<td colspan="3">
					<div id="detailPanel" class="detail-panel">
						<div class="detail-card shadow-sm">
							<button id="detailClose" class="btn-close" aria-label="Close"></button>
							<div class="circle-outer">
								<div class="circle-inner">
									<div id="bpmValue" class="bpm">60.3 bpm</div>
								</div>
							</div>
							<div class="detail-meta text-center text-muted mt-2" id="detailMeta">ecg_record_1.json</div>
							<canvas id="detailChart" width="600" height="240" style="max-width:100%;"></canvas>
						</div>
					</div>
				</td>
			`;
		}

		// return element references
		return {
			panel: detailContainer.querySelector('#detailPanel') || document.getElementById('detailPanel'),
			bpm: detailContainer.querySelector('#bpmValue') || document.getElementById('bpmValue'),
			meta: detailContainer.querySelector('#detailMeta') || document.getElementById('detailMeta'),
			closeBtn: detailContainer.querySelector('#detailClose') || document.getElementById('detailClose'),
			canvas: detailContainer.querySelector('#detailChart') || document.getElementById('detailChart')
		};
	}

	async function showDetailFor(id){
		const rec = records.find(r=>r.id === Number(id));
		if(!rec) return;

		// ensure the detail row and inner elements exist and get refs
		const elems = ensureDetailElements();

		// find row and insert detail panel after it
		const row = document.querySelector(`#recordsTable [data-id="${id}"]`);
		if(row){
			const tr = row.closest('tr');
			tr.after(detailContainer);
		}

		// default value (could be replaced by real measurement)
		elems.bpm.textContent = '60.3 bpm';
		elems.meta.textContent = rec.name;

		// prepare and render chart: x axis is 1000 samples at 250Hz (4s), y from csv 'oi' column
		const sampleCount = 1000;
		const sampleInterval = 1/250; // 0.004s
		const xs = Array.from({length: sampleCount}, (_,i)=> Number((i * sampleInterval).toFixed(3)));

		// load oi column values from CSV (csv_module)
		let ys = [];
		if(window.csvModule && typeof window.csvModule.loadOiColumn === 'function'){
			try{
				const vals = await window.csvModule.loadOiColumn();
				ys = vals.slice(0, sampleCount);
			}catch(e){
				console.error('failed to load oi column', e);
				ys = [];
			}
		}

		// pad or trim to sampleCount
		if(ys.length < sampleCount){
			const pad = new Array(sampleCount - ys.length).fill(0);
			ys = ys.concat(pad);
		} else if(ys.length > sampleCount){
			ys = ys.slice(0, sampleCount);
		}

		if(detailChart){ detailChart.destroy(); detailChart = null; }

		const detailCanvas = elems.canvas;
		if(detailCanvas && window.Chart){
			const ctx = detailCanvas.getContext('2d');
			detailChart = new Chart(ctx, {
				type: 'line',
				data: {
					labels: xs,
					datasets: [{
						label: 'y = x',
						data: ys,
						borderColor: '#0d6efd',
						tension: 0.1,
						fill: false,
						pointRadius: 0
					}]
				},
				options: {
					scales: {
						x: { title: { display: true, text: 'x' } },
						y: { title: { display: true, text: 'y' } }
					},
						plugins: { legend: { display: false } },
						responsive: true,
						maintainAspectRatio: true
				}
			});
		}

		// wire up close button (may be created dynamically)
		const detailClose = elems.closeBtn;
		if(detailClose) detailClose.addEventListener('click', hideDetail);

		// insert and show
		if(detailContainer.classList.contains('d-none')) detailContainer.classList.remove('d-none');
		const panel = elems.panel;
		if(panel) panel.scrollIntoView({behavior:'smooth', block:'center'});
	}

	function hideDetail(){
		detailContainer.classList.add('d-none');
		if(detailChart){ detailChart.destroy(); detailChart = null; }
	}

	// delegate clicks from tbody to handle view button or name clicks
	tbody.addEventListener('click', (ev)=>{
		const btn = ev.target.closest('.view-btn');
		if(btn){
			const id = btn.getAttribute('data-id');
			showDetailFor(id);
			return;
		}

		const cell = ev.target.closest('.name-cell');
		if(cell){
			const id = cell.getAttribute('data-id');
			showDetailFor(id);
		}
	});

	// close button is wired when the detail row is created

	// click outside panel closes it
	document.addEventListener('click', (ev)=>{
		if(detailContainer.classList.contains('d-none')) return;
		const inside = ev.target.closest('#detailPanel') || ev.target.closest('.name-cell') || ev.target.closest('.view-btn');
		if(!inside) hideDetail();
	});
})();

