import axios from 'axios'

export const httpClient = axios.create({
  baseURL: 'http://localhost:5254/api',
  headers: {
    'Content-Type': 'application/json',
  },
})