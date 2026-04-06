fn PassAround[T:! type](input: T) -> T {
    input
}

fn main() -> i32 {
    let x = 42;
    let y = PassAround(x);
    x + y
}
