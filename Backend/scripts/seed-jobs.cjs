/**
 * Script to generate and seed jobs to the TaskAgent API.
 * 
 * Usage:
 *   node scripts/seed-jobs.cjs [count] [apiUrl]
 * 
 * Examples:
 *   node scripts/seed-jobs.cjs 100                           # Generate 100 jobs, post to localhost:5000
 *   node scripts/seed-jobs.cjs 100 http://localhost:5001     # Generate 100 jobs, post to custom URL
 */

const titles = [
  'Foundation pour – Building A', 'Foundation pour – Phase 2', 'Slab pour – Warehouse', 'Slab pour – Retail',
  'Telebelt – wall and slab', 'Telebelt – parking structure', 'Boom pump – footings', 'Boom pump – curb and gutter',
  'HVAC unit lifts – office tower', 'HVAC unit lifts – hospital', 'Steel erection – bay 1', 'Equipment set – rooftop',
  'Mobile crane – generator', 'Crane – mechanical unit', 'Potholing – utilities locate', 'Potholing – design verify',
  'Daylighting – gas line', 'Hydrovac – fiber locate', 'CCTV sewer main – 1200 LF', 'CCTV – storm main',
  'HDD bore – fiber under highway', 'HDD bore – water line', 'Auger boring – 4 casings', 'Geotechnical bore – soil sample',
  'Horizontal drill – conduit', 'Auger – under rail', 'Aggregate haul – base course', 'Aggregate haul – subgrade',
  'Topsoil delivery – 20 loads', 'Heavy haul – transformer', 'Wind turbine blade – heavy haul', 'Equipment haul – excavator',
  'Traffic control – lane closure', 'Traffic control – work zone', 'Lane closure – milling', 'Detour – bridge work',
  'Concrete pump – slab on grade', 'Pump – elevated deck', 'Crane – HVAC RTU', 'Crane – facade panel',
  'Hydrovac – pothole', 'Vacuum excavate – utility crossing', 'Drill – bore under road', 'Bore – creek crossing',
  'Haul – gravel', 'Haul – demolition debris', 'Signage – interstate', 'Lane closure – paving',
]

const descriptions = [
  'Boom pump, 80 yd³. Slab and footings. 36m reach. Access from north lot.',
  'Telebelt 60m. 120 yd³. Parking structure level 2. Chute to formwork.',
  'Mobile crane, 90-ton. (4) RTU replacements, rooftop. Rigging and set.',
  'Hydrovac, 3–4 holes. Expose gas, electric, comms. Paving in 2 days.',
  'Horizontal drill, 350 ft. 4" conduit. DOT bore, traffic control on site.',
  '15 loads, 3/4" minus. Quarry to site. 28 mi one way. End-dump.',
  'Left lane closure, 1.2 mi. 6a–4p. Cones, signage, attenuator. DOT permit.',
  'Oversize, 3 segments. Port to wind farm. Escort, pilot cars. 180 mi.',
  'Hydrovac potholing at manholes, then CCTV push. 15" RCP. Manhole rehab list.',
  'Geotechnical bore, 4× 18" casings under rail. 85 ft each. Jack and bore.',
  'Pump 42m, 65 yd³. Footings and slab. East access. PM pour.',
  'Crane 50-ton. Two RTUs. Rooftop. Rigging and set. 1-day.',
  'Pothole 6 locations. Gas and electric. Verify depths. Backfill same day.',
  'HDD 280 ft. 6" HDPE. Under county road. Bore and pull.',
  '25 loads 3/4" minus. 35 mi. End-dump. Spreader on site.',
  'Right lane, 0.8 mi. 5a–3p. DOT #8842. Attenuator, cones.',
  'Telebelt 42m. 95 yd³. Wall and slab. Level 3. Chute.',
  'Auger 3× 24" casings. 72 ft. Under rail. Jack and bore.',
  'CCTV 800 LF. 18" RCP. 12 manholes. Push camera. Report.',
  'Heavy haul 2 pieces. 120 mi. Escort. Pilot cars. Oversize.',
]

const companies = [
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
  'Denver, CO', 'Houston, TX', 'Phoenix, AZ', 'Dallas, TX', 'Chicago, IL', 'Seattle, WA', 'Atlanta, GA',
  'Portland, OR', 'Boston, MA', 'Kansas City, MO', 'Las Vegas, NV', 'San Antonio, TX', 'San Diego, CA',
  'Minneapolis, MN', 'Tampa, FL', 'Detroit, MI', 'Philadelphia, PA', 'Charlotte, NC', 'Indianapolis, IN',
  'Columbus, OH', 'Miami, FL', 'Austin, TX', 'Nashville, TN', 'Milwaukee, WI', 'Baltimore, MD',
  'Oakland, CA', 'El Paso, TX', 'Memphis, TN', 'Louisville, KY', 'Omaha, NE', 'Albuquerque, NM',
  'Tucson, AZ', 'Fresno, CA', 'Sacramento, CA', 'Mesa, AZ', 'Cleveland, OH', 'Raleigh, NC',
]

const streets = [
  'Main St', 'Oak Ave', 'Industrial Blvd', 'Commerce Dr', 'Highway 50', 'Frontage Rd', 'Parkway',
  'Construction Way', 'Site Access Rd', 'Project Dr', 'E 64th Ave', 'Smith St', 'Washington St',
  'Belt Line Rd', 'Harlem Ave', 'Broad St', 'Union Station approach', 'Decatur St', 'Jefferson Ave',
  'Market St', 'State St', 'Lake Shore Dr', 'I-85 NB mm 94', 'Terminal 4', 'Beacon St', 'Congress Ave',
  '5th Ave', 'River Rd', 'Quarry Rd', 'Mill St', 'Factory Dr', 'Dock St', 'Port Rd',
]

function pick(arr) { return arr[Math.floor(Math.random() * arr.length)] }
function pad(n, w = 3) { return String(n).padStart(w, '0') }

function randomDate(start, end) {
  const s = new Date(start).getTime(), e = new Date(end).getTime()
  return new Date(s + Math.random() * (e - s))
}

function generateJobs(count) {
  const jobs = []
  for (let i = 1; i <= count; i++) {
    const sched = randomDate('2026-02-01', '2026-04-30')
    const created = randomDate('2026-01-01', '2026-01-20')
    const updated = randomDate(created, '2026-01-25')
    const loc = pick(locations)
    const [city, state] = loc.split(', ')
    const num = Math.floor(Math.random() * 9000) + 100
    const zip = String(10000 + Math.floor(Math.random() * 90000))
    const address = `${num} ${pick(streets)}, ${city}, ${state} ${zip}`

    jobs.push({
      id: `JOB-${pad(i)}`,
      title: pick(titles),
      description: pick(descriptions),
      company: pick(companies),
      location: loc,
      address,
      scheduleTime: sched.toISOString(),
      scheduleDate: new Date(sched.toISOString().slice(0, 10) + 'T00:00:00.000Z').toISOString(),
      createdAt: created.toISOString(),
      updatedAt: updated.toISOString(),
    })
  }
  return jobs
}

async function seedJobs(jobs, apiUrl) {
  const response = await fetch(`${apiUrl}/api/jobs/seed`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ jobs }),
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(`Failed to seed jobs: ${response.status} ${response.statusText}\n${text}`)
  }

  return await response.json()
}

async function main() {
  const count = parseInt(process.argv[2]) || 100
  const apiUrl = process.argv[3] || 'http://localhost:5000'

  console.log(`Generating ${count} jobs...`)
  const jobs = generateJobs(count)

  console.log(`Seeding ${jobs.length} jobs to ${apiUrl}...`)
  try {
    const result = await seedJobs(jobs, apiUrl)
    console.log('Success:', result)
  } catch (error) {
    console.error('Error:', error.message)
    process.exit(1)
  }
}

main()
