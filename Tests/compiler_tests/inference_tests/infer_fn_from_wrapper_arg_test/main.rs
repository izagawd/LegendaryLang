struct Wrapper(T:! type) {
    kk: T
}

fn do_something[T:! Copy](something: Wrapper(T)) -> T {
    something.kk
}

fn main() -> i32 {
    do_something(make Wrapper { kk : 5 })
}
