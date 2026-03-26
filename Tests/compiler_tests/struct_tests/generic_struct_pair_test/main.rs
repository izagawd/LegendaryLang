struct Pair<A, B> {
    first: A,
    second: B
}

fn main() -> i32 {
    let p = Pair::<i32, i32> { first = 10, second = 20 };
    p.first + p.second
}
