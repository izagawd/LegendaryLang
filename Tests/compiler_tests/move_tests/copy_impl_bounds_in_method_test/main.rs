struct Wrapper(T:! type) {
    val: T
}

trait Something: Sized {
    fn bruh(input: Self) -> i32;
}

impl[T:! Copy] Copy for Wrapper(T) {}

impl[T:! Copy] Something for Wrapper(T) {
    fn bruh(input: Wrapper(T)) -> i32 {
        let kk = input;
        let dd = input;
        5
    }
}

fn main() -> i32 {
    let w = make Wrapper(i32) { val : 10 };
    (Wrapper(i32) as Something).bruh(w)
}
