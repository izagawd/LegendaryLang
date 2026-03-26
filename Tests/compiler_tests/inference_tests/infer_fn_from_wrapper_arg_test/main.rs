struct Wrapper<T> {
    kk: T
}

fn do_something<T: Copy>(something: Wrapper<T>) -> T {
    something.kk
}

fn main() -> i32 {
    do_something(Wrapper { kk = 5 })
}
