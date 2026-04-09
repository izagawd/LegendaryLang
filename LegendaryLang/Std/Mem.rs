fn SizeOf(T:! type) -> usize;
fn AlignOf(T:! type) -> usize;

struct ManuallyDrop(T:! type) {
    val: T
}

impl[T:! type] ManuallyDrop(T) {
    fn New(val: T) -> ManuallyDrop(T) {
        make ManuallyDrop { val: val }
    }
}
