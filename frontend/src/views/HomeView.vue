<script setup lang="ts">
import { ref } from 'vue'

interface InsuranceRequest {
  houseValue: number
  buildYear: number
  location: string
  constructionType: string
  bedrooms: number
  floodZone?: string
  earthquakeZone?: string
}

interface InsuranceResponse {
  annualPremium: number
  monthlyPremium: number
  riskLevel: string
}

interface LotData {
  address: string
  location: string
  region: string
  floorArea: number
  landArea: number
  bedrooms: number
  bathrooms: number
  buildType: string
  buildYear: number | null
  estimatedHouseValue: number
  mappedLocation: string
  mappedConstructionType: string
  floodZone: string
  earthquakeZone: string
  latitude: number | null
  longitude: number | null
}

const lotId = ref<number | null>(null)
const lotData = ref<LotData | null>(null)
const loadingLot = ref(false)
const lotError = ref<string | null>(null)

const form = ref<InsuranceRequest>({
  houseValue: 500000,
  buildYear: 2000,
  location: 'Auckland',
  constructionType: 'Brick',
  bedrooms: 3
})

const result = ref<InsuranceResponse | null>(null)
const loading = ref(false)

const downloadPDF = async () => {
  if (!lotData.value || !lotId.value) return
  
  try {
    const response = await fetch(`http://localhost:5000/api/lot/${lotId.value}/download-report`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(result.value)
    })
    
    if (!response.ok) {
      throw new Error('Failed to generate PDF')
    }
    
    const blob = await response.blob()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `lot_${lotId.value}_report.pdf`
    document.body.appendChild(a)
    a.click()
    document.body.removeChild(a)
    URL.revokeObjectURL(url)
  } catch (error) {
    console.error('Error downloading PDF:', error)
    alert('Failed to generate PDF report')
  }
}

const downloadPDF_OLD = () => {
  if (!lotData.value) return
  
  const lot = lotData.value
  const quote = result.value
  
  // Create PDF content
  const content = `
LOT ${lotId.value}
DP - 
SITE AREA ${lot.landArea.toFixed(0)}m²

┌─────────────────────┬──────────────┐
│ WIND ZONE           │ HIGH         │
│ EQ ZONE             │ ${lot.earthquakeZone.toUpperCase().padEnd(12)} │
│ FLOOD ZONE          │ ${lot.floodZone.toUpperCase().padEnd(12)} │
│ CLIMATE ZONE        │ 4            │
│ SUBSOIL CLASS       │ -            │
│ PLANNING ZONE       │ RES21        │
└─────────────────────┴──────────────┘

SITE COVERAGE - 50% MAX.

DWELLING (O/CLADDING)    = ${lot.floorArea.toFixed(1)}m²
SITE AREA                = ${lot.landArea.toFixed(0)}m²
                         = ${((lot.floorArea / lot.landArea) * 100).toFixed(1)}%

SITE AREA                = ${lot.landArea.toFixed(0)}m²
DWELLING (O/ROOF)        = ${lot.floorArea.toFixed(1)}m²
BEDROOMS                 = ${lot.bedrooms}
BATHROOMS                = ${lot.bathrooms}

ADDRESS: ${lot.address}
LOCATION: ${lot.location}, ${lot.region}
BUILD TYPE: ${lot.buildType}
${lot.buildYear ? `BUILD YEAR: ${lot.buildYear}` : ''}

COORDINATES: ${lot.latitude?.toFixed(6)}, ${lot.longitude?.toFixed(6)}

${quote ? `
INSURANCE QUOTE
───────────────────────────────────
Annual Premium:  $${quote.annualPremium.toFixed(2)}
Monthly Premium: $${quote.monthlyPremium.toFixed(2)}
Risk Level:      ${quote.riskLevel}
───────────────────────────────────
` : ''}

Generated: ${new Date().toLocaleString()}
`
  
  // Create blob and download
  const blob = new Blob([content], { type: 'text/plain' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `lot_${lotId.value}_report.txt`
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  URL.revokeObjectURL(url)
}

const fetchLotData = async () => {
  if (!lotId.value) return
  
  loadingLot.value = true
  lotError.value = null
  lotData.value = null
  
  try {
    const response = await fetch(`http://localhost:5000/api/lot/${lotId.value}`)
    
    if (!response.ok) {
      throw new Error('Lot not found')
    }
    
    const data: LotData = await response.json()
    lotData.value = data
    
    // Auto-populate form with pre-calculated backend values
    form.value.houseValue = data.estimatedHouseValue
    form.value.buildYear = data.buildYear || new Date().getFullYear()
    form.value.bedrooms = data.bedrooms
    form.value.location = data.mappedLocation
    form.value.constructionType = data.mappedConstructionType
    
  } catch (error) {
    lotError.value = 'Failed to fetch lot data. Please check the Lot ID.'
    console.error('Error:', error)
  } finally {
    loadingLot.value = false
  }
}

const clearLotData = () => {
  lotId.value = null
  lotData.value = null
  lotError.value = null
}

const calculateInsurance = async () => {
  loading.value = true
  try {
    // Include hazard zones if available from lot data
    const requestData = {
      ...form.value,
      floodZone: lotData.value?.floodZone,
      earthquakeZone: lotData.value?.earthquakeZone
    }
    
    const response = await fetch('http://localhost:5000/api/insurance/calculate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(requestData)
    })
    result.value = await response.json()
  } catch (error) {
    console.error('Error:', error)
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="container">
    <h1>NZ House Insurance Calculator</h1>
    
    <!-- Lot ID Lookup Section -->
    <div class="lot-lookup">
      <h3>Import from Lot ID</h3>
      <div class="lot-input-group">
        <input 
          v-model.number="lotId" 
          type="number" 
          placeholder="Enter Lot ID (e.g., 1156)"
          :disabled="loadingLot"
          min="100"
          max="10000"
          class="lot-id-input"
        />
        <button 
          type="button" 
          @click="fetchLotData" 
          :disabled="!lotId || loadingLot"
          class="btn-fetch"
        >
          {{ loadingLot ? 'Loading...' : 'Fetch Lot Data' }}
        </button>
        <button 
          v-if="lotData"
          type="button" 
          @click="clearLotData"
          class="btn-clear"
        >
          Clear
        </button>
      </div>
      
      <div v-if="lotError" class="lot-error">
        {{ lotError }}
      </div>
      
      <div v-if="lotData" class="lot-info">
        <h4>Lot Information</h4>
        <div class="lot-details">
          <div><strong>Address:</strong> {{ lotData.address }}</div>
          <div><strong>Location:</strong> {{ lotData.location }}, {{ lotData.region }}</div>
          <div v-if="lotData.latitude && lotData.longitude">
            <strong>Coordinates:</strong> {{ lotData.latitude.toFixed(4) }}, {{ lotData.longitude.toFixed(4) }}
          </div>
          <div><strong>Floor Area:</strong> {{ lotData.floorArea }} m²</div>
          <div><strong>Land Area:</strong> {{ lotData.landArea }} m²</div>
          <div><strong>Bedrooms:</strong> {{ lotData.bedrooms }}</div>
          <div><strong>Build Type:</strong> {{ lotData.buildType }}</div>
          <div v-if="lotData.buildYear"><strong>Build Year:</strong> {{ lotData.buildYear }}</div>
        </div>
        
        <div class="hazard-zones">
          <h4>Hazard Zones</h4>
          <div class="hazard-item">
            <span>Flood Zone:</span>
            <span :class="'zone-' + lotData.floodZone.toLowerCase()">{{ lotData.floodZone }}</span>
          </div>
          <div class="hazard-item">
            <span>Earthquake Zone:</span>
            <span :class="'zone-' + lotData.earthquakeZone.toLowerCase()">{{ lotData.earthquakeZone }}</span>
          </div>
        </div>
        
        <p class="lot-success">✓ Form auto-populated with lot data</p>
      </div>
    </div>
    
    <form @submit.prevent="calculateInsurance" class="form">
      <div class="form-group">
        <label>House Value (NZD)</label>
        <input v-model.number="form.houseValue" type="number" required />
      </div>

      <div class="form-group">
        <label>Build Year</label>
        <input v-model.number="form.buildYear" type="number" required />
      </div>

      <div class="form-group">
        <label>Location</label>
        <input v-model="form.location" type="text" required placeholder="e.g., Auckland, Wellington" />
      </div>

      <div class="form-group">
        <label>Construction Type</label>
        <select v-model="form.constructionType">
          <option>Brick</option>
          <option>Weatherboard</option>
          <option>Concrete</option>
          <option>Other</option>
        </select>
      </div>

      <div class="form-group">
        <label>Bedrooms</label>
        <input v-model.number="form.bedrooms" type="number" required />
      </div>

      <button type="submit" :disabled="loading">
        {{ loading ? 'Calculating...' : 'Calculate Premium' }}
      </button>
    </form>

    <div v-if="result" class="result">
      <h2>Insurance Quote</h2>
      <div class="result-item">
        <span>Annual Premium:</span>
        <strong>${{ result.annualPremium.toFixed(2) }}</strong>
      </div>
      <div class="result-item">
        <span>Monthly Premium:</span>
        <strong>${{ result.monthlyPremium.toFixed(2) }}</strong>
      </div>
      <div class="result-item">
        <span>Risk Level:</span>
        <strong :class="'risk-' + result.riskLevel.toLowerCase()">{{ result.riskLevel }}</strong>
      </div>
      
      <button 
        v-if="lotData" 
        @click="downloadPDF" 
        class="btn-download"
        type="button"
      >
        📄 Download Property Report (PDF)
      </button>
    </div>
  </div>
</template>

<style scoped>
.container {
  max-width: 800px;
  margin: 0 auto;
  padding: 2rem;
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
}

h1 {
  color: #2c3e50;
  margin-bottom: 2rem;
  text-align: center;
  width: 100%;
}

.lot-lookup {
  background: #fff3cd;
  padding: 2rem;
  border-radius: 8px;
  margin-bottom: 2rem;
  border-left: 4px solid #ffc107;
  width: 100%;
  max-width: 700px;
}

.lot-lookup h3 {
  margin-top: 0;
  color: #856404;
  font-size: 1.3rem;
  text-align: center;
  margin-bottom: 1.5rem;
}

.lot-input-group {
  display: flex;
  gap: 1rem;
  margin-bottom: 1rem;
  align-items: center;
  justify-content: center;
}

.lot-id-input {
  flex: 0 0 200px;
  font-size: 1.2rem;
  padding: 1rem;
  text-align: center;
  font-weight: 600;
}

.btn-fetch, .btn-clear {
  padding: 1rem 2rem;
  border: none;
  border-radius: 4px;
  font-size: 1rem;
  cursor: pointer;
  font-weight: 600;
  white-space: nowrap;
  transition: all 0.2s;
}

.btn-fetch {
  background: #ffc107;
  color: #000;
}

.btn-fetch:hover:not(:disabled) {
  background: #e0a800;
}

.btn-fetch:disabled {
  background: #6c757d;
  color: white;
  cursor: not-allowed;
}

.btn-clear {
  background: #6c757d;
  color: white;
}

.btn-clear:hover {
  background: #5a6268;
}

.lot-error {
  padding: 0.75rem;
  background: #f8d7da;
  color: #721c24;
  border-radius: 4px;
  margin-top: 0.5rem;
}

.lot-info {
  margin-top: 1rem;
  padding: 1rem;
  background: white;
  border-radius: 4px;
}

.lot-info h4 {
  margin-top: 0;
  color: #495057;
}

.lot-details {
  display: grid;
  gap: 0.5rem;
  font-size: 0.9rem;
  color: #495057;
}

.lot-success {
  margin-top: 1rem;
  margin-bottom: 0;
  color: #155724;
  font-weight: 600;
}

.hazard-zones {
  margin-top: 1rem;
  padding: 1rem;
  background: #f8f9fa;
  border-radius: 4px;
  border: 1px solid #dee2e6;
}

.hazard-zones h4 {
  margin-top: 0;
  margin-bottom: 0.75rem;
  color: #495057;
  font-size: 1rem;
}

.hazard-item {
  display: flex;
  justify-content: space-between;
  padding: 0.5rem 0;
  font-size: 0.9rem;
}

.hazard-item span:first-child {
  color: #6c757d;
  font-weight: 600;
}

.zone-high {
  color: #dc3545;
  font-weight: 700;
  padding: 0.25rem 0.75rem;
  background: #f8d7da;
  border-radius: 4px;
}

.zone-medium {
  color: #ffc107;
  font-weight: 700;
  padding: 0.25rem 0.75rem;
  background: #fff3cd;
  border-radius: 4px;
}

.zone-low {
  color: #28a745;
  font-weight: 700;
  padding: 0.25rem 0.75rem;
  background: #d4edda;
  border-radius: 4px;
}

.zone-unknown {
  color: #6c757d;
  font-weight: 600;
  padding: 0.25rem 0.75rem;
  background: #e9ecef;
  border-radius: 4px;
}

.form {
  background: #f8f9fa;
  padding: 2rem;
  border-radius: 8px;
  margin-bottom: 2rem;
  width: 100%;
  max-width: 700px;
}

.form-group {
  margin-bottom: 1.5rem;
}

label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 600;
  color: #495057;
}

input, select {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ced4da;
  border-radius: 4px;
  font-size: 1rem;
}

button {
  width: 100%;
  padding: 1rem;
  background: #007bff;
  color: white;
  border: none;
  border-radius: 4px;
  font-size: 1rem;
  cursor: pointer;
  font-weight: 600;
}

button:hover:not(:disabled) {
  background: #0056b3;
}

button:disabled {
  background: #6c757d;
  cursor: not-allowed;
}

.result {
  background: #e7f3ff;
  padding: 2rem;
  border-radius: 8px;
  border-left: 4px solid #007bff;
  width: 100%;
  max-width: 700px;
}

.result h2 {
  margin-top: 0;
  color: #2c3e50;
}

.result-item {
  display: flex;
  justify-content: space-between;
  padding: 0.75rem 0;
  border-bottom: 1px solid #cce5ff;
}

.result-item:last-child {
  border-bottom: none;
}

.risk-low { color: #28a745; }
.risk-medium { color: #ffc107; }
.risk-high { color: #dc3545; }

.btn-download {
  margin-top: 1.5rem;
  background: #28a745;
  font-size: 1.1rem;
  padding: 1.25rem;
}

.btn-download:hover {
  background: #218838;
}
</style>
