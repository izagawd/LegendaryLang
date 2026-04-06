enum Counter {
    Count(i32)
}

fn increment(c: &mut Counter) {
    match c {
        Counter.Count(n) => *n = *n + 1
    }
}

fn main() -> i32 {
    let c = Counter.Count(0);
    increment(&mut c);
    increment(&mut c);
    increment(&mut c);
    match c {
        Counter.Count(n) => n
    }
}
