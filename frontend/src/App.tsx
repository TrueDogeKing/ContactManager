import { BrowserRouter, Routes, Route } from 'react-router-dom';
import ContactsListPage from './pages/ContactsListPage';
import ContactDetailsPage from './pages/ContactDetailsPage';
import './App.css';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<ContactsListPage />} />
        <Route path="/contacts/:id" element={<ContactDetailsPage />} />
      </Routes>
    </BrowserRouter>
  );
}
