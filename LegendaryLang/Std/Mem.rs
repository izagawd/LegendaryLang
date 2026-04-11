fn SizeOf(T:! Sized) -> usize;
fn AlignOf(T:! Sized) -> usize;

struct ManuallyDrop(T:! Sized) {
    val: T
}

impl[T:! Sized] ManuallyDrop(T) {
    fn New(val: T) -> ManuallyDrop(T) {
        make ManuallyDrop { val: val }
    }

    fn inner_ref(self: &Self) -> &T {
        &self.val
    }

    fn inner_mut(self: &mut Self) -> &mut T {
        &mut self.val
    }

    fn inner(self: Self) -> T {
        self.val
    }
}
