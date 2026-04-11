fn SizeOf(T:! Sized) -> usize;
fn AlignOf(T:! Sized) -> usize;

struct ManuallyDrop(T:! Sized) {
    val: T
}

impl[T:! Sized] ManuallyDrop(T) {
    fn New(val: T) -> ManuallyDrop(T) {
        make ManuallyDrop { val: val }
    }
}
