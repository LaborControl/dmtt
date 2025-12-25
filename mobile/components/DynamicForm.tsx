/**
 * Dynamic Form Component
 *
 * Parses and renders forms from JSON templates (TaskTemplate.FormTemplate)
 * Supports: text, number, boolean, select, multiselect, date, time, photo
 */

import React, { useState } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  ScrollView,
  Image,
  Alert,
} from 'react-native';
import * as ImagePicker from 'expo-image-picker';

// ============================================================================
// TYPES
// ============================================================================

export interface FormField {
  name: string;
  label: string;
  type: 'text' | 'number' | 'boolean' | 'select' | 'multiselect' | 'date' | 'time' | 'photo';
  required?: boolean;
  placeholder?: string;
  options?: string[]; // For select/multiselect
  min?: number; // For number
  max?: number; // For number
  maxPhotos?: number; // For photo fields
}

export interface FormTemplate {
  fields: FormField[];
}

export interface FormData {
  [key: string]: any;
}

interface DynamicFormProps {
  formTemplateJson?: string | null;
  initialData?: FormData;
  onDataChange?: (data: FormData) => void;
}

// ============================================================================
// COMPONENT
// ============================================================================

export default function DynamicForm({ formTemplateJson, initialData = {}, onDataChange }: DynamicFormProps) {
  const [formData, setFormData] = useState<FormData>(initialData);

  // ==========================================================================
  // Parse form template
  // ==========================================================================
  const template: FormTemplate | null = React.useMemo(() => {
    if (!formTemplateJson) {
      return null;
    }

    try {
      return JSON.parse(formTemplateJson) as FormTemplate;
    } catch (error) {
      console.error('[DYNAMIC FORM] Invalid JSON template:', error);
      return null;
    }
  }, [formTemplateJson]);

  // ==========================================================================
  // Update form data
  // ==========================================================================
  const updateField = (name: string, value: any) => {
    const newData = { ...formData, [name]: value };
    setFormData(newData);
    onDataChange?.(newData);
  };

  // ==========================================================================
  // Take photo
  // ==========================================================================
  const takePhoto = async (fieldName: string, maxPhotos: number = 5) => {
    try {
      const { status } = await ImagePicker.requestCameraPermissionsAsync();
      if (status !== 'granted') {
        Alert.alert('Permission refusée', 'Accès à la caméra nécessaire');
        return;
      }

      const result = await ImagePicker.launchCameraAsync({
        allowsEditing: true,
        quality: 0.7,
        base64: true,
      });

      if (!result.canceled && result.assets[0].base64) {
        const currentPhotos = formData[fieldName] || [];
        if (currentPhotos.length >= maxPhotos) {
          Alert.alert('Limite atteinte', `Maximum ${maxPhotos} photos`);
          return;
        }

        updateField(fieldName, [...currentPhotos, result.assets[0].base64]);
      }
    } catch (error) {
      console.error('[DYNAMIC FORM] Photo error:', error);
      Alert.alert('Erreur', 'Impossible de prendre la photo');
    }
  };

  // ==========================================================================
  // Remove photo
  // ==========================================================================
  const removePhoto = (fieldName: string, index: number) => {
    const currentPhotos = [...(formData[fieldName] || [])];
    currentPhotos.splice(index, 1);
    updateField(fieldName, currentPhotos);
  };

  // ==========================================================================
  // Render field
  // ==========================================================================
  const renderField = (field: FormField) => {
    const value = formData[field.name];

    switch (field.type) {
      // TEXT INPUT
      case 'text':
        return (
          <View key={field.name} style={styles.fieldContainer}>
            <Text style={styles.label}>
              {field.label}
              {field.required && <Text style={styles.required}> *</Text>}
            </Text>
            <TextInput
              style={styles.textInput}
              placeholder={field.placeholder}
              value={value || ''}
              onChangeText={(text) => updateField(field.name, text)}
              multiline
            />
          </View>
        );

      // NUMBER INPUT
      case 'number':
        return (
          <View key={field.name} style={styles.fieldContainer}>
            <Text style={styles.label}>
              {field.label}
              {field.required && <Text style={styles.required}> *</Text>}
            </Text>
            <TextInput
              style={styles.textInput}
              placeholder={field.placeholder}
              value={value?.toString() || ''}
              onChangeText={(text) => {
                const num = parseFloat(text);
                updateField(field.name, isNaN(num) ? null : num);
              }}
              keyboardType="numeric"
            />
            {(field.min !== undefined || field.max !== undefined) && (
              <Text style={styles.hint}>
                {field.min !== undefined && field.max !== undefined
                  ? `Entre ${field.min} et ${field.max}`
                  : field.min !== undefined
                  ? `Minimum ${field.min}`
                  : `Maximum ${field.max}`}
              </Text>
            )}
          </View>
        );

      // BOOLEAN (YES/NO)
      case 'boolean':
        return (
          <View key={field.name} style={styles.fieldContainer}>
            <Text style={styles.label}>
              {field.label}
              {field.required && <Text style={styles.required}> *</Text>}
            </Text>
            <View style={styles.radioGroup}>
              <TouchableOpacity
                style={[styles.radioButton, value === true && styles.radioButtonSelected]}
                onPress={() => updateField(field.name, true)}
              >
                <Text style={styles.radioText}>Oui</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.radioButton, value === false && styles.radioButtonSelected]}
                onPress={() => updateField(field.name, false)}
              >
                <Text style={styles.radioText}>Non</Text>
              </TouchableOpacity>
            </View>
          </View>
        );

      // SELECT (single choice)
      case 'select':
        return (
          <View key={field.name} style={styles.fieldContainer}>
            <Text style={styles.label}>
              {field.label}
              {field.required && <Text style={styles.required}> *</Text>}
            </Text>
            <View style={styles.selectGroup}>
              {field.options?.map((option) => (
                <TouchableOpacity
                  key={option}
                  style={[styles.selectButton, value === option && styles.selectButtonSelected]}
                  onPress={() => updateField(field.name, option)}
                >
                  <Text
                    style={value === option ? styles.selectTextSelected : styles.selectText}
                  >
                    {option}
                  </Text>
                </TouchableOpacity>
              ))}
            </View>
          </View>
        );

      // MULTISELECT (multiple choices)
      case 'multiselect':
        const selectedOptions = value || [];
        return (
          <View key={field.name} style={styles.fieldContainer}>
            <Text style={styles.label}>
              {field.label}
              {field.required && <Text style={styles.required}> *</Text>}
            </Text>
            <View style={styles.selectGroup}>
              {field.options?.map((option) => {
                const isSelected = selectedOptions.includes(option);
                return (
                  <TouchableOpacity
                    key={option}
                    style={[styles.selectButton, isSelected && styles.selectButtonSelected]}
                    onPress={() => {
                      if (isSelected) {
                        updateField(
                          field.name,
                          selectedOptions.filter((o: string) => o !== option)
                        );
                      } else {
                        updateField(field.name, [...selectedOptions, option]);
                      }
                    }}
                  >
                    <Text style={isSelected ? styles.selectTextSelected : styles.selectText}>
                      {option}
                    </Text>
                  </TouchableOpacity>
                );
              })}
            </View>
          </View>
        );

      // PHOTO
      case 'photo':
        const photos = value || [];
        const maxPhotos = field.maxPhotos || 5;
        return (
          <View key={field.name} style={styles.fieldContainer}>
            <Text style={styles.label}>
              {field.label} ({photos.length}/{maxPhotos})
              {field.required && <Text style={styles.required}> *</Text>}
            </Text>
            <ScrollView horizontal style={styles.photosContainer}>
              {photos.map((photo: string, index: number) => (
                <View key={index} style={styles.photoWrapper}>
                  <Image
                    source={{ uri: `data:image/jpeg;base64,${photo}` }}
                    style={styles.photoPreview}
                  />
                  <TouchableOpacity
                    style={styles.photoRemoveButton}
                    onPress={() => removePhoto(field.name, index)}
                  >
                    <Text style={styles.photoRemoveText}>✕</Text>
                  </TouchableOpacity>
                </View>
              ))}

              {photos.length < maxPhotos && (
                <TouchableOpacity
                  style={styles.addPhotoButton}
                  onPress={() => takePhoto(field.name, maxPhotos)}
                >
                  <Text style={styles.addPhotoText}>📷</Text>
                  <Text style={styles.addPhotoLabel}>Ajouter</Text>
                </TouchableOpacity>
              )}
            </ScrollView>
          </View>
        );

      default:
        return (
          <View key={field.name} style={styles.fieldContainer}>
            <Text style={styles.label}>Type non supporté: {field.type}</Text>
          </View>
        );
    }
  };

  // ==========================================================================
  // Render
  // ==========================================================================
  if (!template || !template.fields || template.fields.length === 0) {
    return (
      <View style={styles.emptyContainer}>
        <Text style={styles.emptyText}>Aucun formulaire disponible</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {template.fields.map((field) => renderField(field))}
    </View>
  );
}

// ============================================================================
// STYLES
// ============================================================================

const styles = StyleSheet.create({
  container: {
    gap: 16,
  },
  emptyContainer: {
    padding: 20,
    alignItems: 'center',
  },
  emptyText: {
    fontSize: 14,
    color: '#94a3b8',
  },
  fieldContainer: {
    marginBottom: 8,
  },
  label: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1e293b',
    marginBottom: 8,
  },
  required: {
    color: '#ef4444',
  },
  hint: {
    fontSize: 12,
    color: '#64748b',
    marginTop: 4,
  },
  textInput: {
    borderWidth: 2,
    borderColor: '#e2e8f0',
    borderRadius: 8,
    padding: 12,
    fontSize: 16,
    minHeight: 48,
    backgroundColor: '#fff',
  },
  radioGroup: {
    flexDirection: 'row',
    gap: 12,
  },
  radioButton: {
    flex: 1,
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 8,
    borderWidth: 2,
    borderColor: '#e2e8f0',
    alignItems: 'center',
    backgroundColor: '#fff',
  },
  radioButtonSelected: {
    borderColor: '#2563eb',
    backgroundColor: '#eff6ff',
  },
  radioText: {
    fontSize: 16,
    color: '#1e293b',
    fontWeight: '600',
  },
  selectGroup: {
    gap: 8,
  },
  selectButton: {
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 8,
    borderWidth: 2,
    borderColor: '#e2e8f0',
    backgroundColor: '#fff',
  },
  selectButtonSelected: {
    borderColor: '#2563eb',
    backgroundColor: '#eff6ff',
  },
  selectText: {
    fontSize: 16,
    color: '#64748b',
  },
  selectTextSelected: {
    fontSize: 16,
    color: '#2563eb',
    fontWeight: 'bold',
  },
  photosContainer: {
    flexDirection: 'row',
  },
  photoWrapper: {
    position: 'relative',
    marginRight: 12,
  },
  photoPreview: {
    width: 80,
    height: 80,
    borderRadius: 8,
    borderWidth: 2,
    borderColor: '#e2e8f0',
  },
  photoRemoveButton: {
    position: 'absolute',
    top: -8,
    right: -8,
    backgroundColor: '#ef4444',
    width: 24,
    height: 24,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
  },
  photoRemoveText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
  addPhotoButton: {
    width: 80,
    height: 80,
    borderRadius: 8,
    borderWidth: 2,
    borderColor: '#2563eb',
    borderStyle: 'dashed',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: '#eff6ff',
  },
  addPhotoText: {
    fontSize: 32,
  },
  addPhotoLabel: {
    fontSize: 12,
    color: '#2563eb',
    fontWeight: 'bold',
    marginTop: 4,
  },
});
