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

const form = ref<InsuranceRequest>({
  houseValue: 500000,
  buildYear: 2000,
  location: 'Auckland',
  constructionType: 'Brick',
  bedrooms: 3
})

const result = ref<InsuranceResponse | null>(null)
const loading = ref(false)

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
        <select v-model="form.location">
          <option>Auckland</option>
          <option>Wellington</option>
          <option>Christchurch</option>
          <option>Other</option>
        </select>
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
  max-width: 600px;
  margin: 2rem auto;
  padding: 2rem;
}

h1 {
  color: #2c3e50;
  margin-bottom: 2rem;
}

.form {
  background: #f8f9fa;
  padding: 2rem;
  border-radius: 8px;
  margin-bottom: 2rem;
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
