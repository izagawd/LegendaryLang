fn double_use(T:! type, a: T) -> i32 {
    let b = a;
    let c = a;
    5
}

fn main() -> i32 {
    double_use(i32, 10)
}
