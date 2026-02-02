/**
 * Script to generate and seed companies to the TaskAgent API.
 * 
 * Usage:
 *   node scripts/seed-companies.cjs [apiUrl]
 * 
 * Examples:
 *   node scripts/seed-companies.cjs                           # Post to localhost:5001
 *   node scripts/seed-companies.cjs http://localhost:5000     # Post to custom URL
 */

// The 40 companies from the job seed data
const companyNames = [
  'Summit Development', 'Atlas Crane', 'Ground Breakers Hydrovac', 'Bore-All Drilling', 'Riverside Materials',
  'Metro Concrete Co', 'SafeZone Traffic', 'Long-Haul Logistics', 'Municipal Pipe Services', 'RailWorks Drilling',
  'Peak Concrete', 'Valley Crane', 'Delta Hydrovac', 'Premier Drilling', 'Central Aggregates',
  'City Concrete', 'QuickZone Traffic', 'Freight Masters', 'Metro Pipe', 'TransBore',
  'Highland Builders', 'Titan Crane', 'DigSafe Hydrovac', 'CrossBore Drilling', 'Rocky Materials',
  'Urban Concrete', 'Lane Guard Traffic', 'Heavy Haul Inc', 'SewerScope', 'Rail Bore Co',
  'Summit Pumping', 'Skyline Crane', 'VacTec Hydrovac', 'Directional Drilling', 'Gravel Express',
  'FormTech Concrete', 'WorkZone Pros', 'Oversize Logistics', 'PipeVision', 'Tunnel Bore LLC',
]

const locations = [
  { city: 'Denver', state: 'CO' },
  { city: 'Houston', state: 'TX' },
  { city: 'Phoenix', state: 'AZ' },
  { city: 'Dallas', state: 'TX' },
  { city: 'Chicago', state: 'IL' },
  { city: 'Seattle', state: 'WA' },
  { city: 'Atlanta', state: 'GA' },
  { city: 'Portland', state: 'OR' },
  { city: 'Boston', state: 'MA' },
  { city: 'Kansas City', state: 'MO' },
  { city: 'Las Vegas', state: 'NV' },
  { city: 'San Antonio', state: 'TX' },
  { city: 'San Diego', state: 'CA' },
  { city: 'Minneapolis', state: 'MN' },
  { city: 'Tampa', state: 'FL' },
]

function pick(arr) { return arr[Math.floor(Math.random() * arr.length)] }
function pad(n, w = 3) { return String(n).padStart(w, '0') }

// Determine industry based on company name keywords
function getIndustry(name) {
  const nameLower = name.toLowerCase()
  
  if (nameLower.includes('concrete') || nameLower.includes('pumping') || nameLower.includes('formtech')) {
    return 'Concrete'
  }
  if (nameLower.includes('crane')) {
    return 'Crane'
  }
  if (nameLower.includes('hydrovac') || nameLower.includes('vac') || nameLower.includes('digsafe')) {
    return 'Hydrovac'
  }
  if (nameLower.includes('drilling') || nameLower.includes('bore') || nameLower.includes('tunnel')) {
    return 'Drilling'
  }
  if (nameLower.includes('haul') || nameLower.includes('logistics') || nameLower.includes('materials') || 
      nameLower.includes('aggregates') || nameLower.includes('gravel') || nameLower.includes('freight')) {
    return 'Hauling'
  }
  if (nameLower.includes('traffic') || nameLower.includes('zone') || nameLower.includes('lane')) {
    return 'Traffic Control'
  }
  if (nameLower.includes('pipe') || nameLower.includes('sewer') || nameLower.includes('municipal')) {
    return 'Pipe Services'
  }
  if (nameLower.includes('rail')) {
    return 'Rail Services'
  }
  if (nameLower.includes('development') || nameLower.includes('builders') || nameLower.includes('highland')) {
    return 'General Construction'
  }
  
  return 'General Construction'
}

// Generate a random phone number
function generatePhone() {
  const area = Math.floor(Math.random() * 900) + 100
  const prefix = Math.floor(Math.random() * 900) + 100
  const line = Math.floor(Math.random() * 9000) + 1000
  return `(${area}) ${prefix}-${line}`
}

// Generate a random license number
function generateLicense(state) {
  const num = Math.floor(Math.random() * 900000) + 100000
  return `${state}-CON-${num}`
}

// Generate a website URL from company name
function generateWebsite(name) {
  const slug = name.toLowerCase()
    .replace(/[^a-z0-9]+/g, '')
    .slice(0, 20)
  return `https://www.${slug}.com`
}

// Generate email from company name
function generateEmail(name) {
  const slug = name.toLowerCase()
    .replace(/[^a-z0-9]+/g, '')
    .slice(0, 15)
  return `info@${slug}.com`
}

function generateCompanies() {
  const companies = []
  
  for (let i = 0; i < companyNames.length; i++) {
    const name = companyNames[i]
    const location = pick(locations)
    const industry = getIndustry(name)
    
    // ~80% of companies are allowed
    const isAllowed = Math.random() < 0.8
    
    // Random rating between 3.5 and 5.0
    const rating = Math.round((3.5 + Math.random() * 1.5) * 100) / 100
    
    // ~70% have verified insurance
    const insuranceVerified = Math.random() < 0.7
    
    const created = new Date('2025-01-01')
    created.setDate(created.getDate() + Math.floor(Math.random() * 365))
    
    companies.push({
      id: `COMP-${pad(i + 1)}`,
      name,
      isAllowed,
      industry,
      contactEmail: generateEmail(name),
      contactPhone: generatePhone(),
      website: generateWebsite(name),
      city: location.city,
      state: location.state,
      licenseNumber: generateLicense(location.state),
      insuranceVerified,
      rating,
      createdAt: created.toISOString(),
      updatedAt: null,
    })
  }
  
  return companies
}

async function seedCompanies(companies, apiUrl) {
  const response = await fetch(`${apiUrl}/api/companies/seed`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ companies }),
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(`Failed to seed companies: ${response.status} ${response.statusText}\n${text}`)
  }

  return await response.json()
}

async function main() {
  const apiUrl = process.argv[2] || 'http://localhost:5001'

  console.log('Generating companies...')
  const companies = generateCompanies()
  
  const allowed = companies.filter(c => c.isAllowed).length
  const disallowed = companies.filter(c => !c.isAllowed).length
  
  console.log(`Generated ${companies.length} companies:`)
  console.log(`  - ${allowed} allowed`)
  console.log(`  - ${disallowed} disallowed`)
  console.log('')
  
  // Show industry breakdown
  const byIndustry = {}
  companies.forEach(c => {
    byIndustry[c.industry] = (byIndustry[c.industry] || 0) + 1
  })
  console.log('By industry:')
  Object.entries(byIndustry).sort((a, b) => b[1] - a[1]).forEach(([ind, count]) => {
    console.log(`  - ${ind}: ${count}`)
  })
  console.log('')

  console.log(`Seeding to ${apiUrl}...`)
  try {
    const result = await seedCompanies(companies, apiUrl)
    console.log('Success:', result)
    
    console.log('')
    console.log('Disallowed companies (jobs will be hidden):')
    companies.filter(c => !c.isAllowed).forEach(c => {
      console.log(`  - ${c.name} (${c.industry})`)
    })
  } catch (error) {
    console.error('Error:', error.message)
    process.exit(1)
  }
}

main()
