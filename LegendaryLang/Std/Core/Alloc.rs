use Std.Core.Marker.Drop;
use Std.Core.Deref.Receiver;
use Std.Core.Deref.Deref;
use Std.Core.Deref.DerefConst;
use Std.Core.Deref.DerefMut;
use Std.Core.Deref.DerefUniq;

fn SizeOf(T:! type) -> usize;
fn AlignOf(T:! type) -> usize;
fn Alloc(size: usize, align: usize) -> *uniq u8;
fn Dealloc(ptr: *uniq u8, size: usize, align: usize);
fn AllocZeroed(size: usize, align: usize) -> *uniq u8;
fn PtrWrite[T:! type](dst: *uniq u8, val: T) -> *uniq T;
fn PtrAsU8[T:! type](ptr: *uniq T) -> *uniq u8;

struct Box(T:! type) {
    ptr: *uniq T
}

struct ManuallyDrop(T:! type) {
    val: T
}

impl[T:! type] Box(T) {
    fn New(val: T) -> Box(T) {
        let s: usize = SizeOf(T);
        let a: usize = AlignOf(T);
        let raw: *uniq u8 = Alloc(s, a);
        let typed: *uniq T = PtrWrite(raw, val);
        make Box { ptr: typed }
    }

    fn Leak(b: Box(T)) -> &'static uniq T {
        let ptr: *uniq T = b.ptr;
        let _prevent_drop = make ManuallyDrop(Box(T)) { val: b };
        &uniq *ptr
    }
}

impl[T:! type] Drop for Box(T) {
    fn Drop(self: &uniq Self) {
        let p: *uniq u8 = PtrAsU8(self.ptr);
        let s: usize = SizeOf(T);
        let a: usize = AlignOf(T);
        Dealloc(p, s, a);
    }
}

impl[T:! type] Receiver for Box(T) {
    type Target = T;
}

impl[T:! type] Deref for Box(T) {
    fn deref(self: &Self) -> &T {
        &*self.ptr
    }
}

impl[T:! type] DerefConst for Box(T) {
    fn deref_const(self: &const Self) -> &const T {
        &const *self.ptr
    }
}

impl[T:! type] DerefMut for Box(T) {
    fn deref_mut(self: &mut Self) -> &mut T {
        &mut *self.ptr
    }
}

impl[T:! type] DerefUniq for Box(T) {
    fn deref_uniq(self: &uniq Self) -> &uniq T {
        &uniq *self.ptr
    }
}
