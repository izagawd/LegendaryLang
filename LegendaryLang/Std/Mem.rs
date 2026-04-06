fn SizeOf(T:! type) -> usize;
fn AlignOf(T:! type) -> usize;

struct ManuallyDrop(T:! type) {
    val: T
}
