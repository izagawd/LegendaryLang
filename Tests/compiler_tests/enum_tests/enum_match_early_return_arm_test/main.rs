enum Action {
    Go(i32),
    Stop
}

fn process(a: Action) -> i32 {
    match a {
        Action.Go(v) => { return v; },
        Action.Stop => 0
    }
}

fn main() -> i32 {
    process(Action.Go(42))
}
