import axios from 'axios';

const API_BASE = '/api';

export interface Country {
  id: number;
  name: string;
  adjective: string;
  countryId: number;
  governmentType: string;
  authority: string;
  personality: string;
  graphicalCulture: string;
  capital: number;
  militaryPower: number;
  economyPower: number;
  techPower: number;
  fleetSize: number;
  empireSize: number;
  numSapientPops: number;
  victoryRank: number;
  victoryScore: number;
  createdAt: string;
}

export interface CountrySummary {
  country: Country;
  incomeTotals: Record<string, number>;
}

export interface GalaxySummary {
  totals: Record<string, number>;
  previousTotals?: Record<string, number>;
  incomeTotals?: Record<string, number>;
  expenseTotals?: Record<string, number>;
  previousIncomeTotals?: Record<string, number>;
  previousExpenseTotals?: Record<string, number>;
  gameDate?: string;
  previousGameDate?: string | null;
}

export interface Snapshot {
  id: number;
  countryId: number;
  gameDate: string;
  tick: number;
  militaryPower: number;
  economyPower: number;
  techPower: number;
  fleetSize: number;
  empireSize: number;
  numSapientPops: number;
  victoryRank: number;
  victoryScore: number;
  snapshotTime: string;
}

export interface BudgetLineItem {
  id: number;
  snapshotId: number;
  countryId: number;
  section: string;
  category: string;
  resourceType: string;
  amount: number;
}

export interface SpeciesBreakdown {
  speciesName: string;
  amount: number;
}

export const api = {
  getCountries: async (): Promise<Country[]> => {
    const response = await axios.get(`${API_BASE}/countries`);
    return response.data;
  },

  getCountrySummaries: async (): Promise<CountrySummary[]> => {
    const response = await axios.get(`${API_BASE}/countries/summary`);
    return response.data;
  },

  getGalaxySummary: async (): Promise<GalaxySummary> => {
    const response = await axios.get(`${API_BASE}/galaxy/summary`);
    return response.data;
  },

  getCountrySnapshots: async (countryId: number): Promise<Snapshot[]> => {
    const response = await axios.get(`${API_BASE}/countries/${countryId}/snapshots`);
    return response.data;
  },

  getSnapshotBudget: async (snapshotId: number): Promise<BudgetLineItem[]> => {
    const response = await axios.get(`${API_BASE}/snapshots/${snapshotId}/budget`);
    return response.data;
  },

  getSnapshotSpecies: async (snapshotId: number): Promise<SpeciesBreakdown[]> => {
    const response = await axios.get(`${API_BASE}/snapshots/${snapshotId}/species`);
    return response.data;
  },

  getGalaxySpecies: async (): Promise<SpeciesBreakdown[]> => {
    const response = await axios.get(`${API_BASE}/galaxy/species`);
    return response.data;
  },

  getGalaxySpeciesPrevious: async (): Promise<SpeciesBreakdown[]> => {
    const response = await axios.get(`${API_BASE}/galaxy/species/previous`);
    return response.data;
  },

  uploadSaveFile: async (file: File): Promise<any> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await axios.post(`${API_BASE}/saves/upload`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  clearSaveData: async (): Promise<any> => {
    const response = await axios.delete(`${API_BASE}/saves/clear`);
    return response.data;
  },
};
