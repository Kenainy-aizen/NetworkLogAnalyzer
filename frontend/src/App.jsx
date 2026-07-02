import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Dashboard from './pages/Dashboard';
import IpDetail from './pages/IpDetail';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/ip/:ip" element={<IpDetail />} />
      </Routes>
    </BrowserRouter>
  );
}
