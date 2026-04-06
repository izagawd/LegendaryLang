struct Wrapper(T:! type) {
    val: T
}

trait Summable {
    fn sum(a: Self, b: Self) -> i32;
}

impl[T:! Copy] Summable for Wrapper(T) {
    fn sum(a: Wrapper(T), b: Wrapper(T)) -> i32 {
        5
    }
}

fn main() -> i32 {
    let a = make Wrapper(i32) { val : 10 };
    let b = make Wrapper(i32) { val : 20 };
    (Wrapper(i32) as Summable).sum(a, b)
}
