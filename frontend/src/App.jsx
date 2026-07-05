import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import IpDetail from './pages/IpDetail';
import Statistics from './pages/Statistics';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/ip/:ip" element={<IpDetail />} />
        <Route path="/stats" element={<Statistics />} />
      </Routes>
    </BrowserRouter>
  );
}
