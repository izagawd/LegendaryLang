struct Wrapper(T:! type) {
    val: T
}

impl[T:! Copy] Wrapper(T) {
    fn get_val(self: &Self) -> T {
        self.val
    }
}

fn main() -> i32 {
    let w = make Wrapper(i32) { val : 99 };
    w.get_val()
}
