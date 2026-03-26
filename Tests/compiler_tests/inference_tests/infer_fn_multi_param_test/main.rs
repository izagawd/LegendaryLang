struct Pair<A, B> {
    first: A,
    second: B
}

fn make_pair<A: Copy, B: Copy>(a: A, b: B) -> Pair<A, B> {
    Pair::<A, B> { first = a, second = b }
}

fn main() -> i32 {
    let p = make_pair(10, 20);
    p.first + p.second
}
