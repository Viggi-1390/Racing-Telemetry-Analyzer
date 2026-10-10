import React, { useState, useEffect } from 'react';
import { updateVehicle } from '../api/client';

export default function EditVehicleModal({ isOpen, vehicle, onClose, onVehicleUpdated }) {
  const [formData, setFormData] = useState({
    Name: '',
    Manufacturer: '',
    VehicleType: 'Car',
    VehicleCategory: 'Race',
    Class: '',
    Engine: '',
    Power: 0,
    Weight: 0,
    TopSpeed: 0,
  });
  const [imageFile, setImageFile] = useState(null);
  const [previewUrl, setPreviewUrl] = useState(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (vehicle) {
      setFormData({
        Name: vehicle.name || '',
        Manufacturer: vehicle.manufacturer || '',
        VehicleType: vehicle.type === 'bike' ? 'Bike' : 'Car',
        VehicleCategory: vehicle.category === 'production' ? 'Production' : 'Race',
        Class: vehicle.vClass || '',
        Engine: vehicle.engine || '',
        Power: vehicle.power || 0,
        Weight: vehicle.weight || 0,
        TopSpeed: vehicle.topSpeed || 0,
      });
      setPreviewUrl(vehicle.imagePath || null);
      setImageFile(null);
      setError(null);
    }
  }, [vehicle]);

  if (!isOpen || !vehicle) return null;

  function handleChange(e) {
    const { name, value, type } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === 'number' ? (value === '' ? '' : parseInt(value, 10)) : value,
    }));
  }

  function handleImageChange(e) {
    const file = e.target.files?.[0];
    if (file) {
      setImageFile(file);
      setPreviewUrl(URL.createObjectURL(file));
    }
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError(null);

    const name = formData.Name.trim();
    const manufacturer = formData.Manufacturer.trim();
    const power = Number(formData.Power);
    const weight = Number(formData.Weight);
    const topSpeed = Number(formData.TopSpeed);

    if (!name || !manufacturer) {
      setError('Vehicle Name and Manufacturer are required.');
      return;
    }
    if (isNaN(power) || power <= 0 || power > 3000) {
      setError('Horsepower must be between 1 and 3000 HP.');
      return;
    }
    if (isNaN(weight) || weight <= 0 || weight > 10000) {
      setError('Weight must be between 1 and 10000 KG.');
      return;
    }
    if (isNaN(topSpeed) || topSpeed <= 0 || topSpeed > 600) {
      setError('Top speed must be between 1 and 600 KM/H.');
      return;
    }

    try {
      setSaving(true);
      const data = new FormData();
      data.append('VehicleId', vehicle.id);
      data.append('Name', name.toUpperCase());
      data.append('Manufacturer', manufacturer);
      data.append('VehicleType', formData.VehicleType);
      data.append('VehicleCategory', formData.VehicleCategory);
      data.append('Class', formData.Class.trim() || (formData.VehicleType === 'Car' ? 'Race Car' : 'Superbike'));
      data.append('Engine', formData.Engine.trim() || (formData.VehicleType === 'Car' ? 'Hybrid V8' : 'Inline-4'));
      data.append('Power', power);
      data.append('Weight', weight);
      data.append('TopSpeed', topSpeed);

      if (imageFile) {
        data.append('ImageFile', imageFile);
      }

      const res = await updateVehicle(vehicle.id, data);
      if (res && res.success) {
        onVehicleUpdated(res.vehicle);
        onClose();
      } else {
        setError(res?.message || 'Failed to update vehicle specifications.');
      }
    } catch (err) {
      setError(err.message || 'Error communicating with server.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="rta-modal-backdrop" onClick={onClose}>
      <div className="modal-motorsport" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h5 className="modal-title">
            <span style={{ color: '#ff1f38' }}>✏️</span> EDIT VEHICLE SPECIFICATIONS
          </h5>
          <button type="button" className="btn-close-custom" onClick={onClose} aria-label="Close">
            &times;
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            {error && (
              <div style={{ background: 'rgba(255, 31, 56, 0.15)', border: '1px solid #ff1f38', color: '#ff3b50', padding: '10px 14px', borderRadius: '4px', marginBottom: '14px', fontSize: '0.85rem' }}>
                {error}
              </div>
            )}

            {/* IDENTITY */}
            <div className="modal-section-box">
              <div className="modal-section-title">// 01 VEHICLE IDENTITY</div>
              <div className="form-grid-2" style={{ marginBottom: '12px' }}>
                <div>
                  <label className="micro-label">MANUFACTURER</label>
                  <input
                    type="text"
                    name="Manufacturer"
                    value={formData.Manufacturer}
                    onChange={handleChange}
                    className="form-control-dark"
                    required
                  />
                </div>
                <div>
                  <label className="micro-label">VEHICLE NAME</label>
                  <input
                    type="text"
                    name="Name"
                    value={formData.Name}
                    onChange={handleChange}
                    className="form-control-dark"
                    required
                  />
                </div>
              </div>

              <div className="form-grid-4">
                <div>
                  <label className="micro-label">TYPE</label>
                  <select
                    name="VehicleType"
                    value={formData.VehicleType}
                    onChange={handleChange}
                    className="form-control-dark"
                  >
                    <option value="Car">Car</option>
                    <option value="Bike">Bike</option>
                  </select>
                </div>
                <div>
                  <label className="micro-label">CATEGORY</label>
                  <select
                    name="VehicleCategory"
                    value={formData.VehicleCategory}
                    onChange={handleChange}
                    className="form-control-dark"
                  >
                    <option value="Race">Race</option>
                    <option value="Production">Production</option>
                  </select>
                </div>
                <div>
                  <label className="micro-label">CLASS / DIVISION</label>
                  <input
                    type="text"
                    name="Class"
                    value={formData.Class}
                    onChange={handleChange}
                    className="form-control-dark"
                  />
                </div>
                <div>
                  <label className="micro-label">ENGINE / MOTOR</label>
                  <input
                    type="text"
                    name="Engine"
                    value={formData.Engine}
                    onChange={handleChange}
                    className="form-control-dark"
                  />
                </div>
              </div>
            </div>

            {/* PERFORMANCE METRICS */}
            <div className="modal-section-box">
              <div className="modal-section-title">// 02 PERFORMANCE METRICS</div>
              <div className="form-grid-3">
                <div>
                  <label className="micro-label">HORSEPOWER (HP)</label>
                  <input
                    type="number"
                    name="Power"
                    value={formData.Power}
                    onChange={handleChange}
                    className="form-control-dark"
                    min="1"
                    max="3000"
                    required
                  />
                </div>
                <div>
                  <label className="micro-label">WEIGHT (KG)</label>
                  <input
                    type="number"
                    name="Weight"
                    value={formData.Weight}
                    onChange={handleChange}
                    className="form-control-dark"
                    min="1"
                    max="10000"
                    required
                  />
                </div>
                <div>
                  <label className="micro-label">TOP SPEED (KM/H)</label>
                  <input
                    type="number"
                    name="TopSpeed"
                    value={formData.TopSpeed}
                    onChange={handleChange}
                    className="form-control-dark"
                    min="1"
                    max="600"
                    required
                  />
                </div>
              </div>
            </div>

            {/* VEHICLE IMAGE */}
            <div className="modal-section-box">
              <div className="modal-section-title">// 03 VEHICLE IMAGE</div>
              <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
                <div className="edit-img-preview-box">
                  <img
                    src={previewUrl || (formData.VehicleType === 'Bike' ? '/images/placeholder_bike.png' : '/images/placeholder_car.png')}
                    alt="Preview"
                    onError={(e) => {
                      e.target.onerror = null;
                      e.target.src = formData.VehicleType === 'Bike' ? '/images/placeholder_bike.png' : '/images/placeholder_car.png';
                    }}
                  />
                </div>
                <div style={{ flex: 1 }}>
                  <label className="micro-label">CHANGE / UPLOAD IMAGE</label>
                  <input
                    type="file"
                    accept="image/*"
                    onChange={handleImageChange}
                    className="form-control-dark"
                  />
                  <small style={{ color: 'var(--rta-text-secondary, #828a9c)', fontSize: '0.75rem', display: 'block', marginTop: '4px' }}>
                    Leave empty to keep current image. Supports JPG, PNG, WEBP.
                  </small>
                </div>
              </div>
            </div>
          </div>

          <div className="modal-footer">
            <button type="button" className="btn-outline-cancel" onClick={onClose} disabled={saving}>
              CANCEL
            </button>
            <button type="submit" className="btn-red-primary" disabled={saving}>
              {saving ? 'SAVING...' : 'SAVE CHANGES'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

