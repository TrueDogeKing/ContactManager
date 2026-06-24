import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import ProtectedRoute from './auth/ProtectedRoute';
import Header from './components/Header';
import ContactsListPage from './pages/ContactsListPage';
import ContactDetailsPage from './pages/ContactDetailsPage';
import LoginPage from './pages/LoginPage';
import './App.css';

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Header />
        <Routes>
          {/* Public routes */}
          <Route path="/" element={<ContactsListPage />} />
          <Route path="/contacts/:id" element={<ContactDetailsPage />} />
          <Route path="/login" element={<LoginPage />} />

          {/* Protected routes (add/edit contact) are added under this guard in the next points. */}
          <Route element={<ProtectedRoute />}>
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
