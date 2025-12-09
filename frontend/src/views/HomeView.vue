<script setup lang="ts">
import { ref } from 'vue'

interface InsuranceRequest {
  houseValue: number
  buildYear: number
  location: string
  constructionType: string
  bedrooms: number
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
    const response = await fetch('http://localhost:5000/api/insurance/calculate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(form.value)
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
          <div><strong>Floor Area:</strong> {{ lotData.floorArea }} m²</div>
          <div><strong>Land Area:</strong> {{ lotData.landArea }} m²</div>
          <div><strong>Bedrooms:</strong> {{ lotData.bedrooms }}</div>
          <div><strong>Build Type:</strong> {{ lotData.buildType }}</div>
          <div v-if="lotData.buildYear"><strong>Build Year:</strong> {{ lotData.buildYear }}</div>
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
</style>
