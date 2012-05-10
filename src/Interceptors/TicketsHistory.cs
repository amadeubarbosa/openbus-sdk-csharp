namespace tecgraf.openbus.interceptors {
  internal class TicketsHistory {
    private int _base;
    private int _bits;
    private int _index;
    private const int DefaultSize = 32;
    private readonly int _size;

    public TicketsHistory()
      : this(DefaultSize) {
    }

    public TicketsHistory(int size) {
      _base = 0;
      _bits = 0;
      _index = 0;
      _size = size;
    }

    private bool Flag(int index) {
      return (_bits & (1 << index)) != 0;
    }

    private void Set(int index) {
      _bits |= (1 << index);
    }

    private int Clear(int index) {
      return _bits &= ~(1 << index);
    }

    private void DiscardBase() {
      _base++;
      if (_bits != 0) {
        for (int i = 0; i < _size; i++) {
          if (!Flag(_index)) {
            break;
          }
          Clear(_index);
          _index = (_index + 1) % _size;
          _base++;
        }
        _index = (_index + 1) % _size;
      }
    }

    public bool Check(int id) {
      if (id < _base) {
        return false;
      }
      if (id == _base) {
        DiscardBase();
        return true;
      }
      int shift = id - _base - 1;
      if (shift < _size) {
        int idx = (_index + shift) % _size;
        if (Flag(idx)) {
          return false;
        }
        Set(idx);
        return true;
      }
      int extra = shift - _size;
      if (extra < _size) {
        for (int i = 0; i < extra; i++) {
          Clear(_index);
          _index = (_index + 1) % _size;
        }
      }
      else {
        _bits = 0;
        _index = 0;
      }
      _base += extra;
      DiscardBase();
      return Check(id);
    }

    public override string ToString() {
      string str = "{ ";
      for (int i = 0; i < _size-1; i++) {
        if (i == _index) {
          str += "..." + _base + " ";
        }
        else {
          str += " ";
        }
        if (i >= _index) {
          if (Flag(i)) {
            str += "_";
          }
          else {
            str += (_base + i - _index + 1);
          }
        }
        else {
          if (Flag(i)) {
            str += "_";
          }
          else {
            str += (_base + _size - _index + i + 1);
          }
        }
      }
      str += " }";
      return str;
    }
  }
}
