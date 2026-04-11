struct Wrapper(T:! Sized) {
    kk: T
}

fn do_something[T:! Sized +Copy](something: Wrapper(T)) -> T {
    something.kk
}

fn main() -> i32 {
    do_something(make Wrapper { kk : 5 })
}
