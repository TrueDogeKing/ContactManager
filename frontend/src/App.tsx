import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import ProtectedRoute from './auth/ProtectedRoute';
import Header from './components/Header';
import ContactsListPage from './pages/ContactsListPage';
import ContactDetailsPage from './pages/ContactDetailsPage';
import AddContactPage from './pages/AddContactPage';
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

          {/* Protected routes (require authentication) */}
          <Route element={<ProtectedRoute />}>
            <Route path="/contacts/new" element={<AddContactPage />} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
